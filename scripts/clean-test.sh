#!/bin/sh
bake db:setup
rm src/data/result/*
rm src/AnalitF.Net.Test/bin/debug/var/client/data -rf
rm src/AnalitF.Net.Test/bin/debug/var/client/backup -rf
./scripts/test.sh
