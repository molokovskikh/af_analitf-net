#!/bin/sh

bake packages:install GenerateAssemblyInfo
msbuild.exe /nologo /verbosity:quiet src/AnalitF.Net.sln
bake generate:binding:redirection
mkdir src/data
mkdir src/data/update
mkdir src/data/result
mkdir src/data/export
mkdir src/data/ads
mkdir src/data/ads/Воронеж_1
mkdir src/AnalitF.Net.Client/bin/run
cp lib/libmysqld/* src/AnalitF.Net.Test/bin/debug/ -r
cp lib/libmysqld/* src/AnalitF.Net.Client/bin/debug/ -r
cp lib/libmysqld/* src/AnalitF.Net.Client/bin/run/ -r
cp assets/2block.gif src/data/ads/Воронеж_1/
