#!/bin/sh

run=`cygpath -aw src/client/app/bin/run/`
bin=`cygpath -aw src/client/app/bin/debug/`
cd ../../personal/gaurd/src/app/bin/debug/;./ConsoleApplication1.exe --no-stdin $run $bin
