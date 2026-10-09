"""Phase 1 portable runtime verification; stores metrics, never transcripts."""
import argparse,pathlib,time,json,socket,os,csv,re
import numpy as np
from packaging.requirements import Requirement
import importlib.metadata as metadata
parser=argparse.ArgumentParser()
parser.add_argument('--model',type=pathlib.Path,required=True)
parser.add_argument('--output',type=pathlib.Path,required=True)
args=parser.parse_args()
os.environ['HF_HUB_OFFLINE']='1'
def deny(*a,**k):raise RuntimeError('network disabled during portable inference')
socket.socket.connect=deny;socket.socket.connect_ex=deny
issues=[]
for dist in metadata.distributions():
 for item in dist.requires or []:
  req=Requirement(item)
  if req.marker and not req.marker.evaluate({'extra':''}):continue
  try:
   actual=metadata.version(req.name)
   if actual not in req.specifier:issues.append(str(req))
  except metadata.PackageNotFoundError:issues.append(str(req))
if issues:raise RuntimeError('inconsistent portable dependencies: '+repr(issues))
from faster_whisper import WhisperModel
from faster_whisper.audio import decode_audio
import av
root=pathlib.Path.cwd();corpus=root/'.agent-local/phase1/continuation/jamendo-subset'
model=WhisperModel(str(args.model),device='cpu',compute_type='int8',cpu_threads=4,local_files_only=True)
rows=[]
for item in json.loads((corpus/'selection.json').read_text()):
 stem=item['Filepath'][:-4];audio=decode_audio(str(corpus/item['Filepath']),sampling_rate=16000)
 gt=np.genfromtxt(corpus/(stem+'.csv'),delimiter=',',names=True);origin=max(0,float(gt['word_start'][0])-.5);end=origin+29
 start=time.perf_counter();segments,_=model.transcribe(audio[int(origin*16000):int(end*16000)],language='en',word_timestamps=True,vad_filter=False,beam_size=5);segments=list(segments)
 rows.append({'track':stem,'duration_seconds':29,'cpu_inference_seconds':time.perf_counter()-start,'words':sum(len(s.words or []) for s in segments)})
native={name:{'license':entry['license'],'version':entry['version'],'gpl':'--enable-gpl' in entry['configuration'],'nonfree':'--enable-nonfree' in entry['configuration']} for name,entry in av._core.library_meta.items()}
result={'status':'passed','runtime':'CPython 3.12.10 embeddable','dependency_check':'passed','faster_whisper':metadata.version('faster-whisper'),'pyav':av.__version__,
'network_connections_denied':True,'records':rows,'pyav_native':native,'limitations':['same Windows host, not clean VM','CPU tiny only; no transcripts stored']}
args.output.write_text(json.dumps(result,indent=2));print(json.dumps(result))
