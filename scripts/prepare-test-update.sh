update=src/data/update
bake BuildClient
rm $update/*
bake BuildUpdatePackage
cp output/Updater/* $update
echo "99.99.99.99" > $update/version.txt
chmod 644 $update/*
chmod +x $update/*.dll
chmod +x $update/*.exe
