import os,pathlib,time,json,wave,numpy as np
root=pathlib.Path.cwd();work=root/'.agent-local/phase1/continuation'
os.environ['MODEL_PATH']=str(work/'spleeter-models');os.environ['TF_CPP_MIN_LOG_LEVEL']='2'
os.environ['TF_NUM_INTRAOP_THREADS']='4';os.environ['TF_NUM_INTEROP_THREADS']='1'
from spleeter.separator import Separator
with wave.open(str(work/'native-audio.wav')) as w:audio=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').reshape(-1,2).astype('float32')/32768
# controlled mixture: real singer plus original harmonic/percussive accompaniment.
audio=audio[::1];from scipy.signal import resample_poly
audio=resample_poly(audio,147,160,axis=0)[:44100*20]
t=np.arange(len(audio))/44100;backing=.04*np.sin(2*np.pi*130.81*t)+.03*np.sin(2*np.pi*196*t)
backing+=.06*np.sin(2*np.pi*70*t)*np.exp(-((t%0.5)*35));backing=np.repeat(backing[:,None],2,axis=1)
mix=(audio+backing).astype("float32")
def sisdr(ref,est):
 ref=ref.ravel().astype('float64');est=est.ravel().astype('float64');ref-=ref.mean();est-=est.mean()
 target=ref*(np.dot(est,ref)/np.dot(ref,ref));return float(10*np.log10(np.sum(target**2)/max(1e-20,np.sum((est-target)**2))))
begin=time.perf_counter();separator=Separator('spleeter:2stems',multiprocess=False);load=time.perf_counter()-begin
begin=time.perf_counter();result=separator.separate(mix);elapsed=time.perf_counter()-begin
j={'status':'passed','model':'spleeter:2stems v1.4.0','code':'spleeter 2.4.2','device':'CPU','model_setup_seconds':load,'inference_seconds':elapsed,'duration_seconds':len(audio)/44100,
'input_vocal_si_sdr_db':sisdr(audio,mix),'output_vocal_si_sdr_db':sisdr(audio,result['vocals']),
'limitations':['real singing vocadito_6 with synthetic backing, not a commercial multitrack benchmark','Windows dependency override: tensorflow-io-gcs-filesystem 0.31.0 replaces unavailable 0.32.0','no redistribution approval for this environment']}
(work/'separation-results.json').write_text(json.dumps(j,indent=2));print(json.dumps(j))
