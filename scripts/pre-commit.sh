#!/bin/sh

git diff --name-only | /bin/grep .cs | xargs toutf-all.sh
git diff --name-only | xargs clean.sh
git diff --check
bake check:common:error
msbuild.exe /nologo /verbosity:quiet src/*.sln
./scripts/test.sh
