"""Codec-independent native ASR feasibility probe; private raw transcripts, public metrics."""
import argparse,pathlib,json,csv,re,subprocess,time
import numpy as np
import shutil
p=argparse.ArgumentParser()
p.add_argument('--exe',type=pathlib.Path,required=True);p.add_argument('--model',type=pathlib.Path,required=True)
p.add_argument('--codec',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True)
p.add_argument('--prepared-wav',action='store_true')
p.add_argument('--work',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation'))
args=p.parse_args();root=pathlib.Path.cwd();work=args.work;corpus=work/'jamendo-subset';raw=args.output.parent/'whisper-cpp-private';raw.mkdir(parents=True,exist_ok=True)
def wer(a,b):
 prev=list(range(len(b)+1))
 for i,x in enumerate(a,1):
  cur=[i]
  for j,y in enumerate(b,1):cur.append(min(prev[j]+1,cur[-1]+1,prev[j-1]+(x!=y)))
  prev=cur
 return prev[-1]/max(1,len(a))
rows=[]
for item in json.loads((corpus/'selection.json').read_text()):
 stem=item['Filepath'][:-4];wav=raw/(stem+'.wav');base=raw/stem
 if args.prepared_wav:shutil.copyfile(work/'whisper-cpp-private'/(stem+'.wav'),wav)
 if not args.prepared_wav:subprocess.run([str(args.codec),'-nostdin','-y','-i',str(corpus/(stem+'.wav')),'-ar','16000','-ac','1','-c:a','pcm_s16le',str(wav)],check=True,capture_output=True)
 begin=time.perf_counter()
 result=subprocess.run([str(args.exe),'-m',str(args.model),'-f',str(wav),'-ng','-t','4','-l','en','-ojf','-of',str(base),'-dtw','tiny'],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 (raw/(stem+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
 if result.returncode:raise RuntimeError('whisper.cpp failed; inspect local private log')
 j=json.loads(base.with_suffix('.json').read_text(encoding='utf-8'))
 text=' '.join(s['text'] for s in j['transcription'])
 gt=np.genfromtxt(corpus/(stem+'.csv'),delimiter=',',names=True);origin=max(0,float(gt['word_start'][0])-.5);keep=np.flatnonzero((gt['word_start']>=origin)&(gt['word_start']<origin+28.5))
 words=(corpus/(stem+'.words.txt')).read_text(encoding='utf-8').splitlines();reference=' '.join(words[i] for i in keep)
 rows.append({'track':stem,'duration_seconds':29,'process_seconds_including_model_load':time.perf_counter()-begin,'word_error_rate':wer(re.findall(r'\w+',reference.lower()),re.findall(r'\w+',text.lower())),
 'reference_words':len(re.findall(r'\w+',reference.lower())),'segments':len(j['transcription']),'dtw_tokens':sum(len(s.get('tokens',[])) for s in j['transcription'])})
 print('Native ASR measured',stem,flush=True)
result={'status':'passed','engine':'whisper.cpp 1.9.5','model':'multilingual tiny GGML, MIT','device':'CPU','native_build':'CPU source build; CUDA/Vulkan/OpenMP/FFmpeg/BLAS and AVX/AVX2/FMA/F16C disabled',
 'records':rows,'limitations':['three English 29s real music excerpts; no quality ranking against int8 CTranslate2','DTW token timestamps produced, not forced alignment of supplied lyrics','controlled research environment, no clean end-user machine','raw transcript and system info private']}
args.output.write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
