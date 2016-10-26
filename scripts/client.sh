#!/bin/sh

bin=`cygpath -aw src/client/app/bin/debug/AnalitF.Net.Client.exe`
bin-guard --dir share $bin | iconv --from cp866 --to utf-8
