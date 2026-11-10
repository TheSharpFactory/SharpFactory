"""Starts SQL Server in Docker and creates the databases the database tests need.

  SharpFactoryTest     the Coverage schema (Tests/SF.Tests/Fixtures/Coverage.sql) and its model, built from this
                       database by build_reference_model.cs with this server's login  -> SHARPFACTORY_TEST_EDMJ
  SharpFactoryDocs     the Coverage schema again, for the documentation screens        -> SHARPFACTORY_DOCS_DB
  SharpFactoryQuote's  names that need quoting (Tests/SF.Tests/Fixtures/QuotedNames.sql) -> SHARPFACTORY_QUOTED_NAMES_DB

The tests sign in with the sa login and a random password made for this run. On GitHub Actions the settings go
to $GITHUB_ENV for the next steps; otherwise they are printed for you to set. Run from the repository root:

    python .github/scripts/setup_test_databases.py [--port 1433]
    python .github/scripts/setup_test_databases.py --remove

--port maps SQL Server to another port when 1433 is taken; --remove stops and deletes the container.
"""
import argparse
import os
import secrets
import subprocess
import sys
import tempfile
import time

CONTAINER = 'sf-test-sql'
IMAGE = 'mcr.microsoft.com/mssql/server:2022-latest'
SQLCMD = '/opt/mssql-tools18/bin/sqlcmd'
FIXTURES = os.path.join('Tests', 'SF.Tests', 'Fixtures')
DATABASES = [('SharpFactoryTest', 'Coverage.sql'), ('SharpFactoryDocs', 'Coverage.sql'), ("SharpFactoryQuote's", 'QuotedNames.sql')]


def docker(*args, check=True, quiet=False):
    result = subprocess.run(['docker', *args], capture_output=True, text=True)
    if check and result.returncode != 0:
        sys.exit(f"docker {args[0]} failed:\n{result.stdout}{result.stderr}")
    if not quiet and result.stdout.strip():
        print(result.stdout.strip())
    return result


def sqlcmd(password, *args, check=True):
    # the password goes in as SQLCMDPASSWORD, not on the command line
    return docker('exec', '-e', f'SQLCMDPASSWORD={password}', CONTAINER, SQLCMD, '-C', '-b', '-I', '-S', 'localhost', '-U', 'sa',
                  *args, check=check, quiet=True)


def start(port, password):
    docker('rm', '-f', CONTAINER, check=False, quiet=True)
    docker('run', '-d', '--name', CONTAINER, '-e', 'ACCEPT_EULA=Y', '-e', f'MSSQL_SA_PASSWORD={password}',
           '-p', f'{port}:1433', IMAGE, quiet=True)

    deadline = time.time() + 180
    while time.time() < deadline:
        if sqlcmd(password, '-Q', 'SELECT 1', check=False).returncode == 0:
            print(f'SQL Server is up on port {port}')
            return
        time.sleep(3)

    print(docker('logs', CONTAINER, check=False, quiet=True).stdout[-3000:])
    sys.exit('SQL Server did not start in time')


def create_databases(password):
    for name, script in DATABASES:
        quoted = '[' + name.replace(']', ']]') + ']'
        sqlcmd(password, '-Q', f'CREATE DATABASE {quoted}')
        docker('cp', os.path.join(FIXTURES, script), f'{CONTAINER}:/tmp/{script}', quiet=True)
        sqlcmd(password, '-d', name, '-i', f'/tmp/{script}')
        print(f'Created {name} with {script}')


def write_model(server, password, out_folder):
    """The reference model for the parity tests: the Coverage fixture's settings with this server's login, then the
    model built from the database by build_reference_model.cs, saved the way the app saves it."""
    with open(os.path.join(FIXTURES, 'Coverage.edmj'), 'rb') as f:
        text = f.read().decode('utf-8-sig')

    # the app writes the connection on one line, its properties in this order
    old = '{"Server":"localhost","Database":"SharpFactoryTest","DisableIntegratedSecurity":false}'
    if text.count(old) != 1:
        sys.exit('Coverage.edmj does not have exactly one localhost / SharpFactoryTest connection')

    new = ('{"Server":"' + server + '","Database":"SharpFactoryTest","User":"sa","Password":"' + password
           + '","DisableIntegratedSecurity":true}')
    connection = os.path.join(out_folder, 'Connection.edmj')
    with open(connection, 'wb') as f:
        f.write(text.replace(old, new).encode('utf-8'))

    # the reference itself is built from this database (foreign key numbers and object order depend on its history)
    model = os.path.join(out_folder, 'Reference.edmj')
    script = os.path.join('.github', 'scripts', 'build_reference_model.cs')
    result = subprocess.run(['dotnet', 'run', '--file', script, '--', connection, model], capture_output=True, text=True)
    if result.returncode != 0 or not os.path.isfile(model):
        sys.exit(f'Building the reference model failed:\n{result.stdout}{result.stderr}'.replace(password, '***'))
    return model


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--port', type=int, default=1433)
    parser.add_argument('--out', default=os.environ.get('RUNNER_TEMP') or tempfile.gettempdir())
    parser.add_argument('--remove', action='store_true')
    options = parser.parse_args()

    if options.remove:
        docker('rm', '-f', CONTAINER, check=False, quiet=True)
        print(f'Removed {CONTAINER}')
        return

    if not os.path.isfile(os.path.join(FIXTURES, 'Coverage.sql')):
        sys.exit('Run this from the repository root (Tests/SF.Tests/Fixtures was not found)')

    # upper case, lower case and digits (SQL Server's rule), and no ';' (the settings are separated by ';')
    password = 'Sf9' + secrets.token_hex(12)
    if os.environ.get('GITHUB_ACTIONS'):
        print(f'::add-mask::{password}')

    server = 'localhost' if options.port == 1433 else f'localhost,{options.port}'
    start(options.port, password)
    create_databases(password)
    model = write_model(server, password, options.out)

    settings = {
        'SHARPFACTORY_TEST_EDMJ': model,
        'SHARPFACTORY_DOCS_DB': f'{server};SharpFactoryDocs;sa;{password}',
        'SHARPFACTORY_QUOTED_NAMES_DB': f"{server};SharpFactoryQuote's;sa;{password}",
    }

    github_env = os.environ.get('GITHUB_ENV')
    if github_env:
        with open(github_env, 'a', encoding='utf-8', newline='\n') as f:
            for key, value in settings.items():
                f.write(f'{key}={value}\n')
        print('Settings written to GITHUB_ENV: ' + ', '.join(settings))
    else:
        print('Set these before running the tests:')
        for key, value in settings.items():
            print(f'  {key}={value}')


if __name__ == '__main__':
    main()
