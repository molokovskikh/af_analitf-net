#!/bin/sh
wixpath=$(cygpath -wa $(dirname `which candle`))
publisher="Инфорум"
version="0.1"
product="АналитФАРМАЦИЯ"
params=
output=output/setup
input=build

rm -rf $output
mkdir $output

cp $input/* $output/
bake UpdateSetupFiles
cd $output

candle.exe /nologo "setup.wxs" "files.wxs" -d"Setup.TargetPath=setup.msi" -d"Setup.Version=$version" -d"Setup.Product=$product" -d"Setup.Publisher=$publisher" &&\
light.exe /nologo -cultures:ru-ru -out "Setup.msi" "setup.wixobj" "files.wixobj" &&\
candle.exe /nologo "bundle.wxs" -d"Setup.TargetPath=setup.msi" -d"Setup.Version=$version" -d"Setup.Product=$product" -d"Setup.Publisher=$publisher" -ext "$wixpath\WixUtilExtension.dll" -ext "$wixpath\WixBalExtension.dll" &&\
light.exe /nologo -cultures:ru-ru -out "Setup.exe" -ext "$wixpath\WixBalExtension.dll" -ext "$wixpath\WixUtilExtension.dll" "bundle.wixobj"
