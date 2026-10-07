"""Phase 1 process-load probe, no production IPC contract."""
import json,sys,time,wave,os
import numpy as np
from swift_f0 import SwiftF0
with wave.open(sys.argv[1],'rb') as wav:
    if wav.getframerate()!=48000 or wav.getsampwidth()!=2:raise RuntimeError('Probe expects 48kHz PCM16')
    samples=np.frombuffer(wav.readframes(wav.getnframes()),dtype='<i2').astype(np.float32).reshape(-1,wav.getnchannels()).mean(axis=1)/32768
if os.environ.get('ULTRASTAR_PHASE1_UMX_CACHE'):
    import torch
    from openunmix import umxhq
    from scipy.signal import resample_poly
    torch.set_num_threads(4)
    torch.hub.set_dir(os.environ['ULTRASTAR_PHASE1_UMX_CACHE'])
    model=umxhq(device='cpu')
    audio=resample_poly(samples,147,160).astype('float32')
    tensor=torch.from_numpy(np.repeat(audio[None,:],2,axis=0)).unsqueeze(0)
    for trial in range(200):
        start=time.perf_counter()
        with torch.inference_mode():result=model(tensor)
        print(json.dumps({'trial':trial,'model':'umxhq','finite':bool(torch.isfinite(result).all()),'seconds':time.perf_counter()-start}),flush=True)
    raise SystemExit(0)
# Controlled 48k -> 16k decimation is for this load probe only, not production preprocessing.
samples=samples[::3]
detector=SwiftF0(threads=1,spin=False)
for trial in range(200):
    start=time.perf_counter();result=detector.detect(samples,16000)
    print(json.dumps({'trial':trial,'voiced_frames':int(np.sum(result.confidence>=.5)),'seconds':time.perf_counter()-start}),flush=True)