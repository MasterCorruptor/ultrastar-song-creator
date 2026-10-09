import pathlib,time,json,wave,torch,numpy as np
from scipy.signal import resample_poly
from openunmix import umxhq
import argparse
parser=argparse.ArgumentParser()
parser.add_argument('--corpus',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation/jamendo-subset'))
parser.add_argument('--models',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation/umx-models'))
parser.add_argument('--output',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation/umxhq-results.json'))
parser.add_argument('--full',action='store_true')
args=parser.parse_args()
root=pathlib.Path.cwd();corpus=args.corpus
torch.hub.set_dir(str(args.models));torch.set_num_threads(4)
begin=time.perf_counter();model=umxhq(device='cpu');setup=time.perf_counter()-begin
rows=[]
for x in json.loads((corpus/'selection.json').read_text()):
 stem=x['Filepath'][:-4]
 with wave.open(str(corpus/(stem+('.full.wav' if args.full else '.wav')))) as w:a=np.frombuffer(w.readframes(w.getnframes()),'<i2').reshape(-1,2).astype('float32')/32768
 a=resample_poly(a,147,160,axis=0).astype('float32');tensor=torch.from_numpy(a.T).unsqueeze(0)
 begin=time.perf_counter()
 finite=True;chunks=0
 # Sequential non-overlapping 30s windows bound this research benchmark's memory.
 # Boundaries are not crossfaded: this measures throughput, not finished separation quality.
 with torch.inference_mode():
  for part in tensor.split(44100*30,dim=-1):
   stems=model(part);finite=finite and bool(torch.isfinite(stems).all());chunks+=1
 elapsed=time.perf_counter()-begin
 rows.append({'track':stem,'duration_seconds':len(a)/44100,'inference_seconds':elapsed,'real_time_factor':elapsed/(len(a)/44100),'finite':finite,'chunks_30s':chunks,'targets':int(stems.shape[1])})
 print('OpenUnmix measured',stem,flush=True)
result={'status':'passed','code':'openunmix 1.3.0 / torch+torchaudio 2.10.0+cpu','model':'umxhq 1.0.1','model_license':'MIT','model_doi':'10.5281/zenodo.3370489','setup_seconds':setup,'records':rows,
'limitations':['29s excerpts or full songs with sequential 30s chunks; no source-stem ground truth or crossfade; excerpt audio is mono duplicated to stereo','CPU only; cannot rank quality against Spleeter','research probe, not production adapter']}
args.output.write_text(json.dumps(result,indent=2));print(json.dumps(result))
