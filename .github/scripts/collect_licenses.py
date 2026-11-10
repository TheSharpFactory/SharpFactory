"""Copies the license and notice files of every NuGet package a project uses into one folder.

Used by release.yml: every download gets a `licenses` folder next to the executable, with the files the packages
themselves ship (LICENSE, NOTICE, THIRD-PARTY-NOTICES). Files with the same content are copied once; INDEX.txt
lists every package and where its files are.

    python collect_licenses.py <output folder> <project.assets.json> [<project.assets.json> ...]
"""
import hashlib
import json
import os
import re
import shutil
import sys

NOTICE_FILE = re.compile(r'(licen[cs]e|notice|third[-_ ]?party)', re.IGNORECASE)
SKIP_EXTENSIONS = ('.nupkg', '.sha512', '.p7s', '.metadata', '.png', '.dll', '.so', '.dylib', '.a', '.json', '.xml')


def package_files(assets_path):
    with open(assets_path, encoding='utf-8') as f:
        assets = json.load(f)

    roots = list(assets.get('packageFolders', {}))
    for key, library in sorted(assets['libraries'].items(), key=lambda item: item[0].lower()):
        if library.get('type') != 'package':
            continue

        name = key.split('/')[0]
        for relative in library.get('files', []):
            # only files at the package root: LICENSE.txt, THIRD-PARTY-NOTICES.TXT, NOTICE, ...
            if '/' in relative or not NOTICE_FILE.search(relative) or relative.lower().endswith(SKIP_EXTENSIONS):
                continue

            for root in roots:
                source = os.path.join(root, library['path'], relative)
                if os.path.isfile(source):
                    yield name, relative, source
                    break


def main(output, assets_paths):
    os.makedirs(output, exist_ok=True)
    copied = {}
    index = []
    seen = set()

    for assets_path in assets_paths:
        for name, relative, source in package_files(assets_path):
            if (name, relative) in seen:
                continue
            seen.add((name, relative))

            with open(source, 'rb') as f:
                digest = hashlib.sha256(f.read()).hexdigest()

            if digest in copied:
                index.append(f'{name}: {relative} is the same as {copied[digest]}')
                continue

            target = f'{name}/{relative}'
            os.makedirs(os.path.join(output, name), exist_ok=True)
            shutil.copyfile(source, os.path.join(output, name, relative))
            copied[digest] = target
            index.append(f'{name}: {target}')

    with open(os.path.join(output, 'INDEX.txt'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('License and notice files shipped by the packages in this download.\n')
        f.write('See THIRD-PARTY-NOTICES.md (next to this folder) for every component and its license.\n\n')
        f.write('\n'.join(index) + '\n')

    print(f'{len(copied)} files copied to {output} ({len(index)} entries)')


if __name__ == '__main__':
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2:])
