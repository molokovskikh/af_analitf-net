update=src/data/update
bake BuildClient
rm $update/*
bake BuildUpdatePackage
cp output/Updater/* $update
cp src/AnalitF.Net.Client/App.config $update/AnalitF.Net.Client.exe.config
echo "99.99.99.99" > $update/version.txt
chmod 644 $update/*
chmod +x $update/*.dll
chmod +x $update/*.exe
