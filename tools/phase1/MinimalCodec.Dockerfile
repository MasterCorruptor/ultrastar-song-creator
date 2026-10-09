FROM ultrastar-phase1-linux
RUN sed -i 's|http://|https://|g' /etc/apt/sources.list.d/ubuntu.sources && apt-get -o Acquire::Retries=1 -o Acquire::https::Timeout=20 update && apt-get -o Acquire::Retries=1 -o Acquire::https::Timeout=20 install -y --no-install-recommends gcc libc6-dev gcc-mingw-w64-x86-64 g++-mingw-w64-x86-64 make nasm xz-utils && rm -rf /var/lib/apt/lists/*
