#!/bin/sh
wixpath=$(cygpath -wa "$(dirname "`which candle`")")
output=output/setup
input=build

rm -rf $output
mkdir $output

cp $input/* $output/
bake setup:preprocess
cd $output

version=`cat version.txt`
publisher="Инфорум"
product="АналитФАРМАЦИЯ"

candle.exe /nologo "setup.wxs" "files.wxs" -d"Setup.TargetPath=setup.msi" -d"Setup.Version=$version" -d"Setup.Product=$product" -d"Setup.Publisher=$publisher" -ext "$wixpath\WixUtilExtension.dll" &&\
light.exe /nologo -cultures:ru-ru -sice:ICE91 -sice:ICE39 -sice:ICE38 -out "Setup.msi" "setup.wixobj" "files.wixobj" &&\
candle.exe /nologo "bundle.wxs" -d"Setup.TargetPath=setup.msi" -d"Setup.Version=$version" -d"Setup.Product=$product" -d"Setup.Publisher=$publisher" -ext "$wixpath\WixUtilExtension.dll" -ext "$wixpath\WixBalExtension.dll" &&\
light.exe /nologo -cultures:ru-ru -out "Setup.exe" -ext "$wixpath\WixBalExtension.dll" -ext "$wixpath\WixUtilExtension.dll" "bundle.wixobj"
