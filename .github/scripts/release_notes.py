"""Prints the CHANGELOG.md section of one version, for the GitHub release page.

Used by release.yml. Fails when the section is missing or empty, so a release never goes out without notes.

    python release_notes.py <version> [CHANGELOG.md]
"""
import re
import sys


def section(changelog, version):
    with open(changelog, encoding='utf-8-sig') as f:
        lines = f.read().replace('\r\n', '\n').split('\n')

    heading = re.compile(r'^## ' + re.escape(version) + r'(\s|$)')
    body = None
    for line in lines:
        if body is None:
            if heading.match(line):
                body = []
            continue
        if line.startswith('## '):
            break
        body.append(line)

    if body is None:
        sys.exit(f"{changelog} has no section '## {version}'; write it before tagging v{version}")

    text = '\n'.join(body).strip()
    if text.endswith('---'):
        text = text[:-3].rstrip()
    if not text:
        sys.exit(f"The '## {version}' section of {changelog} is empty")
    return text + '\n'


if __name__ == '__main__':
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    sys.stdout.write(section(sys.argv[2] if len(sys.argv) > 2 else 'CHANGELOG.md', sys.argv[1]))
