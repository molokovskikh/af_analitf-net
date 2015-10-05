#!/bin/sh
bake db:setup
rm src/data/result/*
rm src/client/test/bin/debug/var/client/result -rf
rm src/client/test/bin/debug/var/client/data -rf
rm src/client/test/bin/debug/var/client/backup -rf
rm src/client/test/bin/debug/var/service/certificates/*
./scripts/test.sh
