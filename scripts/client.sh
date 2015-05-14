#!/bin/sh

run=`cygpath -aw src/client/app/bin/run/`
bin=`cygpath -aw src/client/app/bin/debug/AnalitF.Net.Client.exe`
bin-guard --no-stdin $run $bin
