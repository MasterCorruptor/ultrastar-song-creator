import pathlib,json,time,re
import numpy as np
import soundfile as sf
import librosa
from swift_f0 import SwiftF0,segment_notes
from faster_whisper import WhisperModel
import argparse
from scipy.sparse.csgraph import maximum_bipartite_matching
from scipy.sparse import csr_matrix
parser=argparse.ArgumentParser(description='Phase 1 real solo singing benchmark; no audio/lyrics/transcripts in output.')
parser.add_argument('--corpus',type=pathlib.Path,required=True)
parser.add_argument('--model',type=pathlib.Path,required=True)
parser.add_argument('--output',type=pathlib.Path,required=True)
args=parser.parse_args()
root=pathlib.Path.cwd();corpus=args.corpus
output=args.output;output.mkdir(parents=True,exist_ok=True)
model_path=args.model
model=WhisperModel(str(model_path),device='cpu',compute_type='int8',cpu_threads=4,local_files_only=True)
detector=SwiftF0(threads=1,spin=False)
metadata=__import__('csv').DictReader((corpus/'vocadito_metadata.csv').open())
rows=[]
def wer(a,b):
 prev=list(range(len(b)+1))
 for i,x in enumerate(a,1):
  cur=[i]
  for j,y in enumerate(b,1):cur.append(min(prev[j]+1,cur[-1]+1,prev[j-1]+(x!=y)))
  prev=cur
 return prev[-1]/max(1,len(a))
for item in metadata:
 id=item['track_id'];audio,sr=sf.read(corpus/f'Audio/vocadito_{id}.wav',dtype='float32');audio=audio.mean(axis=1) if audio.ndim>1 else audio
 audio=librosa.resample(audio,orig_sr=sr,target_sr=16000);gt=np.loadtxt(corpus/f'Annotations/F0/vocadito_{id}_f0.csv',delimiter=',');duration=len(audio)/16000
 for kind in ['swift-f0','pyin']:
  begin=time.perf_counter()
  if kind=='swift-f0':
   prediction=detector.detect(audio,16000,fmin=50,fmax=1200);times=prediction.timestamps;f0=prediction.pitch_hz;voiced=prediction.confidence>=.5;notes=segment_notes(prediction)
  else:
   f0,voiced,_=librosa.pyin(audio,sr=16000,fmin=50,fmax=1200,frame_length=2048,hop_length=256);times=librosa.times_like(f0,sr=16000,hop_length=256);notes=[]
  elapsed=time.perf_counter()-begin
  note_scores={}
  if kind=='swift-f0':
   for annotator in ['A1','A2']:
    ref=np.atleast_2d(np.loadtxt(corpus/f'Annotations/Notes/vocadito_{id}_notes{annotator}.csv',delimiter=','))
    if notes:
     start_notes=np.array([n.start for n in notes]);pitch_notes=np.array([n.pitch_hz for n in notes])
     valid=(np.abs(ref[:,0,None]-start_notes[None,:])<=.05)&(np.abs(1200*np.log2(ref[:,1,None]/pitch_notes[None,:]))<=50)
     matching=maximum_bipartite_matching(csr_matrix(valid),perm_type='column');matches=int(np.sum(matching>=0))
    else:matches=0
    precision=matches/max(1,len(notes));recall=matches/max(1,len(ref))
    note_scores[annotator]={'onset_pitch_precision':precision,'onset_pitch_recall':recall,'onset_pitch_f1':2*precision*recall/max(1e-20,precision+recall),'reference_notes':len(ref)}
  right=np.clip(np.searchsorted(gt[:,0],times),0,len(gt)-1)
  left=np.clip(right-1,0,len(gt)-1)
  indices=np.where(np.abs(gt[left,0]-times)<=np.abs(gt[right,0]-times),left,right);reference=gt[indices,1];rv=reference>0;pv=voiced & np.isfinite(f0)&(f0>0);both=rv&pv
  cents=np.full(len(f0),np.inf);cents[both]=1200*np.abs(np.log2(f0[both]/reference[both]));correct=cents<50
  rows.append({'track_id':id,'language':item['language'],'detector':kind,'duration_seconds':duration,'seconds':elapsed,'real_time_factor':elapsed/duration,
   'raw_pitch_accuracy':float(np.sum(correct)/max(1,np.sum(rv))), 'voicing_recall':float(np.sum(both)/max(1,np.sum(rv))), 'voicing_false_alarm':float(np.sum(pv&~rv)/max(1,np.sum(~rv))),
   'median_cents_when_both_voiced':float(np.median(cents[both])) if np.any(both) else None,'proposed_notes':len(notes) if kind=='swift-f0' else None,'note_scores':note_scores})
 if item['language']=='English':
  reference=(corpus/f'Annotations/Lyrics/vocadito_{id}_lyrics.txt').read_text(encoding='utf-8')
  start=time.perf_counter();segments,_=model.transcribe(audio,language='en',beam_size=5,word_timestamps=True,vad_filter=False);segments=list(segments)
  text=' '.join(s.text for s in segments)
  rows.append({'track_id':id,'task':'singing-asr','reference_word_count':len(re.findall(r"\w+",reference.lower())),
    'word_error_rate':wer(re.findall(r"\w+",reference.lower()),re.findall(r"\w+",text.lower())),'seconds':time.perf_counter()-start,
    'word_timestamps_count':sum(len(s.words or[]) for s in segments), 'lyrics_or_transcript_stored':False})
 (output/'vocadito-results.json').write_text(json.dumps({'dataset':'vocadito v3','doi':'10.5281/zenodo.5578807','license':'CC-BY-4.0','records':rows,
 'limitations':['solo singing, no accompaniment','nearest annotation-frame sampling','no word-time ground truth','tiny ASR CPU only','macro aggregation; not duration-weighted','note matching one-to-one, onset 50 ms and pitch 50 cents, no offset requirement']},indent=2),encoding='utf-8')
 print('Completed singing clip',id,flush=True)
print('DONE',len(rows),'measurements')