#!/bin/sh
path=src/AnalitF.Net.Test/bin/Debug
path=$(cygpath -aw $path)
mysqld --console --standalone --port=3310 --datadir="${path/\\/\//}"&
