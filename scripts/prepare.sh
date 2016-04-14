#!/bin/sh

set -o errexit

if [ "`whoami`" != "tester"  ]; then
	git submodule update --init
fi;
bake prepare
msbuild.exe /nologo /verbosity:quiet src/*.sln
if [ "`whoami`" != "tester"  ]; then
	bake db:setup
	bake db:local:seed
fi
mkdir src/data || :
mkdir src/data/update || :
mkdir src/data/result || :
mkdir src/data/export || :
mkdir src/data/ads || :
mkdir src/data/ads/Воронеж_1 || :
mkdir src/client/app/bin/run || :
chmod -R o+w src/data
cp lib/libmysqld/* src/client/test/bin/debug/ -r
cp lib/libmysqld/* src/client/app/bin/debug/ -r
cp lib/libmysqld/* src/client/app/bin/run/ -r
cp assets/2block.gif src/data/ads/Воронеж_1/
cp assets/index.gif src/data/ads/Воронеж_1/
