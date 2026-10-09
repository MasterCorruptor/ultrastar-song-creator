"""Direct CTranslate2 alignment on PCM, with two unchanged MIT helpers; no PyAV import."""
import argparse,pathlib,json,re,wave,time
import sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import ctranslate2,numpy as np,tokenizers
from vendor.faster_whisper.feature_extractor import FeatureExtractor
from vendor.faster_whisper.tokenizer import Tokenizer
p=argparse.ArgumentParser();p.add_argument('--model',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--corpus',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation/jamendo-subset'))
p.add_argument('--pcm',type=pathlib.Path,default=pathlib.Path('.agent-local/phase1/continuation/whisper-cpp-private'))
args=p.parse_args()
corpus=args.corpus;pcm=args.pcm
model=ctranslate2.models.Whisper(str(args.model),device='cpu',compute_type='int8',intra_threads=4)
tok=Tokenizer(tokenizers.Tokenizer.from_file(str(args.model/'tokenizer.json')),True,task='transcribe',language='en')
extractor=FeatureExtractor();rows=[]
for item in json.loads((corpus/'selection.json').read_text()):
 stem=item['Filepath'][:-4]
 with wave.open(str(pcm/(stem+'.wav'))) as w:a=np.frombuffer(w.readframes(w.getnframes()),'<i2').astype('float32')/32768
 times=np.genfromtxt(corpus/(stem+'.csv'),delimiter=',',names=True);origin=max(0,float(times['word_start'][0])-.5)
 keep=np.flatnonzero((times['word_start']>=origin)&(times['word_start']<origin+28.5));words=(corpus/(stem+'.words.txt')).read_text(encoding='utf-8').splitlines()
 tokens=tok.encode(' '+' '.join(words[i] for i in keep))
 start=time.perf_counter();features=extractor(a);encoded=model.encode(ctranslate2.StorageView.from_array(np.expand_dims(features,0)))
 result=model.align(encoded,tok.sot_sequence,[tokens],min(len(a)//160,3000),median_filter_width=7)[0]
 aligned=np.asarray(result.alignments);jumps=np.pad(np.diff(aligned[:,0]),(1,0),constant_values=1).astype(bool);jump_times=aligned[:,1][jumps]/50
 groups,group_tokens=tok.split_to_word_tokens(tokens+[tok.eot]);boundaries=np.pad(np.cumsum([len(x) for x in group_tokens[:-1]]),(1,0))
 predicted=[re.sub(r'[^\w]','',x).lower() for x in groups[:-1]]
 gt=[re.sub(r'[^\w]','',words[i]).lower() for i in keep];errors=[];position=0
 for word,expected in zip(gt,times['word_start'][keep]-origin):
  found=next((k for k in range(position,min(len(predicted),position+4)) if predicted[k]==word),None)
  if found is not None:errors.append(abs(jump_times[boundaries[found]]-expected));position=found+1
 rows.append({'track':stem,'reference_words':len(gt),'matched_words':len(errors),'seconds':time.perf_counter()-start,
 'median_onset_error_seconds':float(np.median(errors)),'p95_onset_error_seconds':float(np.percentile(errors,95))})
out={'status':'passed','engine':'CTranslate2 4.8.2 direct API','decoder':'PCM16 via minimal FFmpeg; no PyAV/faster-whisper package imported',
'reused_helpers':'two unchanged MIT faster-whisper 1.2.1 modules','records':rows,
'limitations':['29s known-text windows, not full-song production aligner','same poor tail timing as tiny alignment baseline','raw lyrics not stored']}
args.output.write_text(json.dumps(out,indent=2),encoding='utf-8');print(json.dumps(out))
