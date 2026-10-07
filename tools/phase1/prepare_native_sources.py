"""Pinned research source/model preparation for whisper.cpp and minimal FFmpeg."""
import pathlib,hashlib,urllib.request,tarfile,json
work=pathlib.Path('.agent-local/phase1/continuation');work.mkdir(parents=True,exist_ok=True)
items=[
('whisper-cpp-1.9.5.tar.gz','https://github.com/ggml-org/whisper.cpp/archive/refs/tags/v1.9.5.tar.gz','ff1a9053feb509ff9d7729703355541ae9690073a6b1c40eb692c962e0dc1720','whisper-cpp-source'),
('ggml-tiny.bin','https://huggingface.co/ggerganov/whisper.cpp/resolve/5359861c739e955e79d9a303bcbc70fb988958b1/ggml-tiny.bin','be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21',None),
]
for name,url,expected,folder in items:
 p=work/name
 if not p.exists():urllib.request.urlretrieve(url,p)
 assert hashlib.sha256(p.read_bytes()).hexdigest()==expected,'unexpected research artifact'
 if folder:
  dest=work/folder;dest.mkdir(exist_ok=True)
  with tarfile.open(p) as t:
   for m in t.getmembers():
    m.name=m.name.split('/',1)[-1]
    if not m.name:continue
    if not (dest/m.name).resolve().is_relative_to(dest.resolve()) or m.issym() or m.islnk():raise ValueError('unsafe archive')
    t.extract(m,dest,filter='data')
 print('Prepared verified research artifact',name)
# FFmpeg archive is an exact Git commit; record digest for comparison to published evidence.
sha='330caae0c1acccd2222edc52a05940c574561ce5'
p=work/'minimal-ffmpeg-source.tar.gz';url='https://github.com/FFmpeg/FFmpeg/archive/'+sha+'.tar.gz'
if not p.exists():urllib.request.urlretrieve(url,p)
dest=work/'minimal-ffmpeg-source';dest.mkdir(exist_ok=True)
with tarfile.open(p) as t:
 for m in t.getmembers():
  m.name=m.name.split('/',1)[-1]
  if not m.name:continue
  if not (dest/m.name).resolve().is_relative_to(dest.resolve()) or m.issym() or m.islnk():raise ValueError('unsafe source')
  t.extract(m,dest,filter='data')
(work/'minimal-ffmpeg-source.json').write_text(json.dumps({'source_commit':sha,'source_url':url,'source_archive_sha256':hashlib.sha256(p.read_bytes()).hexdigest()},indent=2))
