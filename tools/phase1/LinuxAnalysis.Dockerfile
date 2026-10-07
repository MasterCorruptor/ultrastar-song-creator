FROM ultrastar-phase1-linux
RUN /eval/bin/pip install torch==2.10.0+cpu torchaudio==2.10.0+cpu --index-url https://download.pytorch.org/whl/cpu
RUN /eval/bin/pip install openunmix==1.3.0 scipy==1.18.1
