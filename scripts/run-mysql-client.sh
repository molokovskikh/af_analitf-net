#!/bin/sh
path=src/AnalitF.Net.Client/bin/run
path=$(cygpath -aw $path)
mysqldb=$(dirname $(dirname $(which mysqld)))/data/mysql
cp $mysqldb $path -r
mysqld --console --standalone --port=3310 --datadir="${path/\\/\//}"&
mysql --user=root --port=3310
