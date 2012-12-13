#!/bin/sh

bake InstallPackages
bake GenerateAssemblyInfo
bake packages
msbuild.exe /nologo /verbosity:quiet src/AnalitF.Net.sln
mkdir data
mkdir data/update
mkdir data/result
mkdir data/export
mkdir src/AnalitF.Net.Client/bin/run
cp lib/libmysqld/* src/AnalitF.Net.Client.Test/bin/debug/ -r
cp lib/libmysqld/* src/AnalitF.Net.Client/bin/debug/ -r
cp lib/libmysqld/* src/AnalitF.Net.Client/bin/run/ -r
