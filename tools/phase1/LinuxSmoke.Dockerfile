FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-venv xvfb xauth libx11-6 libice6 libsm6 libfontconfig1 libfreetype6 libasound2t64 libxrandr2 libxi6 libxcursor1 libgl1 libpulse0 && rm -rf /var/lib/apt/lists/*
RUN python3 -m venv /eval && /eval/bin/pip install numpy==2.5.3 swift-f0==0.3.0 onnxruntime==1.30.0
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /project
