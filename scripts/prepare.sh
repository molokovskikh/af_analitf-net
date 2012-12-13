#!/bin/sh

bake InstallPackages
bake GenerateAssemblyInfo
bake packages
msbuild.exe /nologo /verbosity:quiet src/AnalitF.Net.sln
mkdir src/data
mkdir src/data/update
mkdir src/data/result
mkdir src/data/export
mkdir src/AnalitF.Net.Client/bin/run
cp lib/libmysqld/* src/AnalitF.Net.Test/bin/debug/ -r
cp lib/libmysqld/* src/AnalitF.Net.Client/bin/debug/ -r
cp lib/libmysqld/* src/AnalitF.Net.Client/bin/run/ -r
