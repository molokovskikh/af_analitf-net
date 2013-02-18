#!/bin/sh

bake InstallPackages
bake GenerateAssemblyInfo
bake packages
msbuild.exe /nologo /verbosity:quiet src/AnalitF.Net.sln
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
cp assets/Common.Models.Tests.config src/Common.Models/Common.Models.Tests/App.config
