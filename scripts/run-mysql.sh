#!/bin/sh
path=$1
path=$(cygpath -aw $path)
defaults=$(cygpath -aw ./scripts/my.ini)
defaults=${defaults/\\/\//}
mysqldb=$(dirname $(dirname $(which mysqld)))/data/mysql
cp $mysqldb $path -r

mysqld --defaults-file="${defaults}" --console --standalone --port=3310 --datadir="${path/\\/\//}"&
mysql data --user=root --port=3310 "${@:2}"
mysqladmin --user=root --port=3310 shutdown
