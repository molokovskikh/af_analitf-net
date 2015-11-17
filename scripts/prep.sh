#!/bin/bash

if [ "$1" == "reset" ]
then
	mysql --execute "update customers.users set targetversion = 1987 where login = 'kvasov'"
	exit
fi

if [ "$1" == "set" ]
then
	mysql --execute "update customers.users set targetversion = null where login = 'kvasov'"
	exit
fi

mysql --execute "update customers.users set targetversion = null where login = 'kvasov'"
mkdir -p /cygdrive/c/inetpub/wwwroot/Results/Updates/Release1988/Exe/
bake build:client env=local
cp output/migration/AnalitF.exe /cygdrive/c/inetpub/wwwroot/Results/Updates/Release1988/Exe/
cp output/migration/AnalitF.exe.config /cygdrive/c/inetpub/wwwroot/Results/Updates/Release1988/Exe/
