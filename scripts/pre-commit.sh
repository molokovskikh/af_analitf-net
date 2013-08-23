#!/bin/sh

git diff --name-only | /bin/grep .cs | xargs toutf-all.sh
git diff --name-only | xargs clean.sh
git diff --check
bake CheckWritingErrors
msbuild.exe /nologo /verbosity:quiet src/AnalitF.Net.sln
./scripts/test.sh
