#!/bin/sh
set -eu
root=/project/.agent-local/phase1/continuation
export GIT_CEILING_DIRECTORIES="$root"
cd "$root/minimal-ffmpeg-source"
./configure --prefix="$root/minimal-codec" --target-os=mingw32 --arch=x86_64 --cross-prefix=x86_64-w64-mingw32- --enable-cross-compile --enable-shared --disable-static --disable-autodetect --disable-everything --disable-doc --disable-debug --disable-network --disable-ffplay --disable-avdevice --disable-swscale --enable-protocol=file --enable-demuxer=mp3,wav,flac,ogg,aac,mov --enable-decoder=mp3float,pcm_s16le,pcm_s24le,pcm_s32le,pcm_f32le,flac,vorbis,aac --enable-parser=mpegaudio,flac,aac --enable-encoder=pcm_s16le --enable-muxer=wav --enable-filter=aresample,aformat,anull --extra-ldflags=-static-libgcc
make -j4
make install
mkdir -p "$root/minimal-codec/source"
cp "$root/minimal-ffmpeg-source.tar.gz" "$root/minimal-codec/source/"
cp "$root/minimal-ffmpeg-source.json" "$root/minimal-codec/source/"
cp /project/tools/phase1/build_minimal_codec.sh "$root/minimal-codec/source/"
cp COPYING.LGPLv2.1 "$root/minimal-codec/"
