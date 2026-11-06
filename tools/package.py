#!/usr/bin/env python3
"""Package source, manifest and generated assets for HalfGripTools. No game DLLs."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import json

root = Path(__file__).resolve().parents[1]
mod = root / 'Obrez'
version = json.loads((mod / 'mod.json').read_text())['version']
destination = root / 'dist' / f'HalfGripObrez-{version}.zip'
destination.parent.mkdir(exist_ok=True)
with ZipFile(destination, 'w', ZIP_DEFLATED) as archive:
    for path in sorted(mod.rglob('*')):
        if path.is_file():
            archive.write(path, Path('Obrez') / path.relative_to(mod))
    archive.write(root / 'README.md', 'README.md')
    for path in sorted((root / 'docs').glob('*')):
        if path.is_file():
            archive.write(path, Path('docs') / path.name)
print(destination)
