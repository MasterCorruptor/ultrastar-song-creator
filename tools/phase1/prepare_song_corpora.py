"""Download the explicitly selected, licensed Phase 1 evaluation corpora and checkpoints."""
import argparse,csv,hashlib,json,pathlib,tarfile,urllib.parse,urllib.request,zipfile
parser=argparse.ArgumentParser()
parser.add_argument('--output',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation'))
args=parser.parse_args();out=args.output;out.mkdir(parents=True,exist_ok=True)
def download(url,path):
 if not path.exists():urllib.request.urlretrieve(url,path)
def safe_extract(archive,destination):
 destination.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(archive) as z:
  for name in z.namelist():
   if not (destination/name).resolve().is_relative_to(destination.resolve()):raise ValueError('unsafe archive path')
  z.extractall(destination)
record=json.load(urllib.request.urlopen('https://zenodo.org/api/records/5578807'))
artifact=next(f for f in record['files'] if f['key']=='vocadito.zip')
p=out/'vocadito.zip';download(artifact['links']['self'],p)
assert 'md5:'+hashlib.md5(p.read_bytes()).hexdigest()==artifact['checksum']
safe_extract(p,out/'vocadito')
base='https://huggingface.co/datasets/jamendolyrics/jamendolyrics/resolve/de188c963fd4539bc769b3feb83582e5a9595e36/'
metadata=out/'JamendoLyrics.csv';download(base+'JamendoLyrics.csv',metadata)
songs=[r for r in csv.DictReader(metadata.open(encoding='utf-8')) if r['Language']=='English' and r['LicenseType'] in ['BY','BY-SA']]
subset=out/'jamendo-subset';subset.mkdir(exist_ok=True)
for r in songs:
 stem=r['Filepath'][:-4]
 for path in ['subsets/en/mp3/'+r['Filepath'],'annotations/words/'+stem+'.csv','lyrics/'+stem+'.words.txt']:
  download(base+urllib.parse.quote(path),subset/pathlib.Path(path).name)
(subset/'selection.json').write_text(json.dumps(songs,indent=2),encoding='utf-8')
record=json.load(urllib.request.urlopen('https://zenodo.org/api/records/3370489'))
cache=out/'umx-models/checkpoints';cache.mkdir(parents=True,exist_ok=True)
models=[]
for f in record['files']:
 if not f['key'].endswith('.pth'):continue
 p=cache/f['key'];download(f['links']['self'],p)
 assert 'md5:'+hashlib.md5(p.read_bytes()).hexdigest()==f['checksum']
 models.append({'filename':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'license':'MIT','source':f['links']['self']})
(out/'corpus-provenance.json').write_text(json.dumps({'vocadito_doi':'10.5281/zenodo.5578807','vocadito_license':'CC-BY-4.0',
 'jamendo_revision':'de188c963fd4539bc769b3feb83582e5a9595e36','umxhq_doi':'10.5281/zenodo.3370489','models':models},indent=2),encoding='utf-8')
print('Phase 1 corpora and MIT UMXHQ models ready; audio and lyrics remain local.')
