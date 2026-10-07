import pathlib,time,json,csv,re,wave
import numpy as np,librosa
from faster_whisper import WhisperModel
import av
def decode_audio(path,sampling_rate=16000):
 resampler=av.AudioResampler(format='flt',layout='mono',rate=sampling_rate)
 frames=[]
 with av.open(path) as container:
  for frame in container.decode(audio=0):
   frames.extend(f.to_ndarray().ravel() for f in resampler.resample(frame))
  frames.extend(f.to_ndarray().ravel() for f in resampler.resample(None))
 return np.concatenate(frames)
from faster_whisper.tokenizer import Tokenizer
root=pathlib.Path.cwd();work=root/'.agent-local/phase1/continuation';corpus=work/'jamendo-subset'
model_path=next((root/'.agent-local/phase1/model-cache/models--Systran--faster-whisper-tiny/snapshots').iterdir())
model=WhisperModel(str(model_path),device='cpu',compute_type='int8',cpu_threads=4,local_files_only=True)
tokenizer=Tokenizer(model.hf_tokenizer,model.model.is_multilingual,task='transcribe',language='en')
records=[]
for item in json.loads((corpus/'selection.json').read_text()):
 name=item['Filepath'];stem=name[:-4];audio=decode_audio(str(corpus/name),sampling_rate=16000)
 times=np.genfromtxt(corpus/(stem+'.csv'),delimiter=',',names=True)
 words=(corpus/(stem+'.words.txt')).read_text().splitlines()
 origin=max(0,float(times['word_start'][0])-.5);end=origin+29
 keep=np.flatnonzero((times['word_start']>=origin)&(times['word_start']<end-.5))
 clip=audio[int(origin*16000):int(end*16000)]
 text=' '.join(words[i] for i in keep);tokens=tokenizer.encode(' '+text)
 features=model.feature_extractor(clip);begin=time.perf_counter();encoded=model.encode(features)
 aligned=model.find_alignment(tokenizer,[tokens],encoded,min(len(clip)//160,3000))[0];elapsed=time.perf_counter()-begin
 # Split-to-words tokenizer can change lexical groups; compare normalized sequential words, report matching coverage.
 gt=[re.sub(r'[^\w]','',words[i]).lower() for i in keep]
 predicted=[re.sub(r'[^\w]','',x['word']).lower() for x in aligned]
 errors=[];position=0
 for word,expected in zip(gt,times['word_start'][keep]-origin):
  if position<len(predicted) and predicted[position]==word:errors.append(abs(aligned[position]['start']-expected));position+=1
  else:
   found=next((k for k in range(position,min(len(predicted),position+4)) if predicted[k]==word),None)
   if found is not None:errors.append(abs(aligned[found]['start']-expected));position=found+1
 records.append({'track':stem,'song_license':item['LicenseType'],'duration_seconds':len(clip)/16000,'known_text_words':len(gt),
 'matched_words':len(errors),'alignment_seconds':elapsed,'word_onset_median_error_seconds':float(np.median(errors)) if errors else None,
 'word_onset_p95_error_seconds':float(np.percentile(errors,95)) if errors else None,
 'word_onsets_within_200ms':float(np.mean(np.array(errors)<=.2)) if errors else None})
 # Private 48k PCM fixture for actual mixed-song separation testing.
 out=librosa.resample(clip,orig_sr=16000,target_sr=48000)
 with wave.open(str(corpus/(stem+'.wav')),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(48000);w.writeframes(np.repeat(np.clip(out*32767,-32768,32767).astype('<i2')[:,None],2,axis=1).tobytes())
 print('Aligned real song',stem,flush=True)
result={'status':'measured','dataset':'JamendoLyrics MultiLang','dataset_revision':'de188c963fd4539bc769b3feb83582e5a9595e36',
'model':'faster-whisper-tiny d90ca5fe260221311c53c58e660288d3deb8d356','records':records,
'limitations':['three preselected English BY/BY-SA songs, first 29 seconds from first annotated word minus 0.5 s','known correct lyrics supplied; not ASR-generated text',
'private find_alignment API, not a supported production adapter','word onset only; no offset or full-song/global repeat evaluation','no lyrics or transcript stored in result']}
(work/'alignment-results.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
