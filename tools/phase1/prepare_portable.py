"""Build the isolated Windows CPU research package, never a product release."""
import argparse,hashlib,json,pathlib,shutil,subprocess,sys,urllib.request,zipfile
p=argparse.ArgumentParser();p.add_argument('--output',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation/portable'))
args=p.parse_args();root=pathlib.Path.cwd().resolve();out=args.output.resolve();work=root/'.agent-local/phase1/continuation'
if not out.is_relative_to(root) or out==root:raise ValueError('Output must stay within project')
out.mkdir(parents=True,exist_ok=True)
archive=work/'python-3.12.10-embed-amd64.zip';url='https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip'
if not archive.exists():urllib.request.urlretrieve(url,archive)
if hashlib.sha256(archive.read_bytes()).hexdigest()!='4acbed6dd1c744b0376e3b1cf57ce906f9dc9e95e68824584c8099a63025a3c3':raise ValueError('Unexpected Python artifact')
py=out/'python';py.mkdir(exist_ok=True)
with zipfile.ZipFile(archive) as z:
 for name in z.namelist():
  if not (py/name).resolve().is_relative_to(py.resolve()):raise ValueError('Unsafe archive path')
 z.extractall(py)
(py/'python312._pth').write_text('python312.zip\n.\nLib/site-packages\nimport site\n',encoding='utf-8')
site=py/'Lib/site-packages'
# Rebuilding package output is explicit; remove only this validated generated package directory.
if site.exists():
 if not site.resolve().is_relative_to(out):raise ValueError('Unexpected package directory')
 shutil.rmtree(site)
subprocess.run([sys.executable,'-m','pip','install','--target',str(site),'--cache-dir',str(root/'.agent-local/phase1/pip-cache'),'--extra-index-url','https://download.pytorch.org/whl/cpu','-r',str(root/'tools/phase1/requirements-portable-cpu.txt')],check=True)
worker=out/'worker';worker.mkdir(exist_ok=True)
for name in ['analysis_worker_probe.py','openunmix_probe.py','direct_alignment_probe.py','whisper_cpp_probe.py','portable_dependencies_probe.py']:
 shutil.copyfile(root/'tools/phase1'/name,worker/name)
shutil.copytree(root/'tools/phase1/vendor',worker/'vendor',dirs_exist_ok=True,ignore=shutil.ignore_patterns('__pycache__','*.pyc'))
asr=out/'asr';asr.mkdir(exist_ok=True)
for f in (work/'whisper-cpp-build/bin/Release').iterdir():
 if f.suffix in ['.exe','.dll']:shutil.copyfile(f,asr/f.name)
shutil.copytree(work/'minimal-codec',out/'codec',dirs_exist_ok=True)
models=out/'models';models.mkdir(exist_ok=True)
shutil.copyfile(work/'ggml-tiny.bin',models/'ggml-tiny.bin')
shutil.copytree(work/'umx-models',models/'umxhq',dirs_exist_ok=True)
ct2=list((root/'.agent-local/phase1/model-cache/models--Systran--faster-whisper-tiny/snapshots').iterdir())
if len(ct2)!=1:raise ValueError('Provide exactly the pinned tiny snapshot')
shutil.copytree(ct2[0],models/'ct2-tiny',dirs_exist_ok=True)
notices=out/'notices';notices.mkdir(exist_ok=True)
shutil.copyfile(py/'LICENSE.txt',notices/'CPython-LICENSE.txt')
shutil.copyfile(work/'whisper-cpp-source/LICENSE',notices/'whisper-cpp-LICENSE')
shutil.copyfile(root/'tools/phase1/vendor/faster_whisper/LICENSE',notices/'reused-faster-whisper-helpers-LICENSE')
for f in site.rglob('*'):
 if f.is_file() and any(k in f.name.lower() for k in ['license','notice','copying']):
  name=str(f.relative_to(site)).replace('\\','_').replace('/','_')
  shutil.copyfile(f,notices/name)
lock=json.loads((root/'tools/phase1/NativeProbe/packages.lock.json').read_text())
for name,entry in lock['dependencies']['net10.0'].items():
 package=root/'.agent-local/phase1/nuget'/name.lower()/entry['resolved']
 for f in package.rglob('*'):
  if f.is_file() and any(k in f.name.lower() for k in ['license','notice','copying']):shutil.copyfile(f,notices/(name+'-'+f.name))
for runtime in (root/'.agent-local/phase1/nuget/microsoft.netcore.app.runtime.win-x64').iterdir():
 for f in runtime.rglob('*'):
  if f.is_file() and any(k in f.name.lower() for k in ['license','notice']):shutil.copyfile(f,notices/('dotnet-runtime-'+f.name))
(out/'results').mkdir(exist_ok=True)
(out/'RESEARCH_ONLY.txt').write_text('Phase 1 local prototype, not a release. No stock PyAV/faster-whisper package. Models/data/notices/source need final artifact-specific release review. See docs/PHASE1_LICENSES.md.',encoding='utf-8')
subprocess.run([str(py/'python.exe'),str(worker/'portable_dependencies_probe.py')],check=True)
print('CPU research package prepared; no public binary/model publication.')
