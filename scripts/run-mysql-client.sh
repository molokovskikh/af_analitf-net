#!/bin/sh
path=src/AnalitF.Net.Client/bin/run
path=$(cygpath -aw $path)
mysqld --console --standalone --port=3310 --datadir="${path/\\/\//}"&
