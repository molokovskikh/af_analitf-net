#!/bin/sh

git diff --name-only | /bin/grep .cs | toutf.sh
git diff --cached --name-only | xargs clean.sh
git diff --check --cached
bake CheckWritingErrors
./scripts/test.sh
