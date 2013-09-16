#!/bin/sh

run=`cygpath -aw src/AnalitF.Net.Client/bin/run/`
bin=`cygpath -aw src/AnalitF.Net.Client/bin/debug/`
cd ../../experiments/gaurd/src/app/bin/debug/;./ConsoleApplication1.exe $run $bin
