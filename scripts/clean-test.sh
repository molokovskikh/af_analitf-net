#!/bin/sh
rm src/AnalitF.Net.Test/bin/debug/data -rf
rm src/AnalitF.Net.Test/bin/debug/backup -rf
./scripts/test.sh
