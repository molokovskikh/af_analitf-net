#!/bin/sh

set -o errexit

bake prepare
msbuild.exe /nologo /verbosity:quiet src/*.sln
mkdir -p src/data/ads/Воронеж_1 || :
chmod -R o+w src/data
cp assets/2block.gif src/data/ads/Воронеж_1/
cp assets/index.gif src/data/ads/Воронеж_1/
