"""Download a pinned, verified .NET SDK for Windows evaluation, under this repo only."""
import hashlib
import json
import pathlib
import urllib.request
import zipfile

SDK_VERSION = '10.0.401'
ROOT = pathlib.Path(__file__).resolve().parents[2] / '.agent-local/phase1'
ROOT.mkdir(parents=True, exist_ok=True)
url = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
with urllib.request.urlopen(url, timeout=30) as response:
    metadata = json.load(response)
sdk = next(candidate for release in metadata['releases']
           for candidate in release.get('sdks', [release.get('sdk', {})])
           if candidate.get('version') == SDK_VERSION)
file = next(item for item in sdk['files'] if item.get('rid') == 'win-x64'
            and item['name'].endswith('.zip'))
archive = ROOT / 'dotnet-sdk.zip'
if not archive.exists():
    urllib.request.urlretrieve(file['url'], archive)
actual = hashlib.sha512(archive.read_bytes()).hexdigest()
if actual.lower() != file['hash'].lower():
    raise RuntimeError('SDK SHA512 differs; preserve the archive for diagnosis and stop')
install = ROOT / 'dotnet'
install.mkdir(exist_ok=True)
with zipfile.ZipFile(archive) as package:
    for item in package.infolist():
        if not (install / item.filename).resolve().is_relative_to(install.resolve()):
            raise RuntimeError('SDK archive contains a path outside the project-local install')
    package.extractall(install)
(ROOT / 'dotnet-sdk.json').write_text(json.dumps({'version': SDK_VERSION, 'url': file['url'],
    'sha512': actual}, indent=2) + '\n', encoding='utf-8')
print(f'PASS: SHA512-verified .NET SDK {SDK_VERSION} under repository .agent-local/')
