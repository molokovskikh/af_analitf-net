#!/bin/sh

run=`cygpath -aw src/client/app/bin/run/`
bin=`cygpath -aw src/client/app/bin/debug/`
cd scripts/guard;./guard.exe --no-stdin $run $bin
