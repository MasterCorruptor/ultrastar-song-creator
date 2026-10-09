FROM ultrastar-phase1-linux-analysis
RUN sed -i 's|http://|https://|g' /etc/apt/sources.list.d/ubuntu.sources && apt-get -o Acquire::Retries=1 -o Acquire::https::Timeout=20 update && apt-get -o Acquire::Retries=1 -o Acquire::https::Timeout=20 install -y --no-install-recommends cmake make g++ libc6-dev && rm -rf /var/lib/apt/lists/*
RUN /eval/bin/pip install ctranslate2==4.8.2 tokenizers==0.23.2 PyYAML==6.0.3
