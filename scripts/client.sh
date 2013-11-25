#!/bin/sh

run=`cygpath -aw src/AnalitF.Net.Client/bin/run/`
bin=`cygpath -aw src/AnalitF.Net.Client/bin/debug/`
cd ../../personal/gaurd/src/app/bin/debug/;./ConsoleApplication1.exe --no-stdin $run $bin
