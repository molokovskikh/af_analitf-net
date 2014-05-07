#!/bin/sh

set errexit

bake prepare
msbuild.exe /nologo /verbosity:quiet src/*.sln
bake generate:binding:redirection
mkdir src/data
mkdir src/data/update
mkdir src/data/result
mkdir src/data/export
mkdir src/data/ads
mkdir src/data/ads/Воронеж_1
mkdir src/client/app/bin/run
cp lib/libmysqld/* src/client/test/bin/debug/ -r
cp lib/libmysqld/* src/client/app/bin/debug/ -r
cp lib/libmysqld/* src/client/app/bin/run/ -r
cp assets/2block.gif src/data/ads/Воронеж_1/
