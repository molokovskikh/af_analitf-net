#!/bin/sh
path=src/AnalitF.Net.Client/bin/run
path=$(cygpath -aw $path)
defaults=$(cygpath -aw ./scripts/my.ini)
defaults=${defaults/\\/\//}
mysqldb=$(dirname $(dirname $(which mysqld)))/data/mysql
cp $mysqldb $path -r
mysqld --defaults-file="${defaults}" --console --standalone --port=3310 --datadir="${path/\\/\//}"&
mysql --user=root --port=3310
