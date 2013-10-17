#!/bin/sh
bake db:setup
rm src/data/result/*
rm src/AnalitF.Net.Test/bin/debug/data -rf
rm src/AnalitF.Net.Test/bin/debug/backup -rf
./scripts/test.sh
