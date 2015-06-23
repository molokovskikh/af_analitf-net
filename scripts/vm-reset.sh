#!/bin/bash

version=$1
if [[ -z "$version" ]]; then
	echo "Укажи версию ос для сброса, доступно xp, 7, 8"
	exit
fi
src="win$version-base-box"
name=win$version
path="$USERPROFILE/data/$name/$name.vbox"
VBoxManage controlvm $name poweroff
VBoxManage unregistervm $name --delete
vboxmanage clonevm "$src" --name $name
vboxmanage registervm "$path"
vboxmanage sharedfolder add $name --name local --hostpath `cygpath -wa .`
vboxmanage startvm $name
