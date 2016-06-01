#!/bin/sh

set -o errexit

wixpath=$(cygpath -wa "$(dirname "`which candle`")")
output=output/setup
input=src/setup

rm -rf $output
mkdir $output

cp $input/* $output/
bake setup:preprocess
cd $output

version=`cat version.txt`
publisher="АналитФармация"
product="АналитФармация"
quiet=""

for i in "$@"; do
	case "$i" in
		--quiet)
			quiet="/q"
			;;
		--version=*)
			version="${i#*=}"
			;;
		esac
	shift
done

candle.exe /nologo "setup.wxs" "files.wxs" -d"Setup.TargetPath=setup.msi" -d"Setup.Version=$version" -d"Setup.Product=$product" -d"Setup.Publisher=$publisher" &&\
light.exe /nologo -cultures:ru-ru -sice:ICE91 -sice:ICE39 -sice:ICE38 -out "Setup.msi" "setup.wixobj" "files.wixobj" &&\
candle.exe /nologo "bundle.wxs" -d"Setup.TargetPath=setup.msi" -d"Setup.Version=$version" -d"Setup.Product=$product" -d"Setup.Publisher=$publisher" -d"Setup.NetInstallCommand=$quiet" -ext "$wixpath\WixBalExtension.dll" -ext "$wixpath\WixUtilExtension.dll" &&\
light.exe /nologo -cultures:ru-ru -out "afsetup.exe" -ext "$wixpath\WixBalExtension.dll" "bundle.wixobj"
