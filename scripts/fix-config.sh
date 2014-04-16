#!/bin/sh

env=$1
if [ "$env" == "local" ]; then
	url="http://localhost:8080"
fi
if [ "$env" == "test" ]; then
	url="http://test.analit.net/AnalitF.Net.Service.Test/"
fi
if [ "$env" == "production" ]; then
	url="http://ios.analit.net/AnalitF.Net.Service/"
fi

if [ -z $url ]; then
	echo "неизвестаня среда $env, доступно local, test, production"
	exit 1
fi


url=${url//\//\\\/}
expr="s/add key=\"Uri\" value=\".*\"/add key=\"Uri\" value=\"$url\"/"
sed -i "$expr" src/client/app/bin/run/AnalitF.Net.Client.exe.config
./scripts/touch.sh
