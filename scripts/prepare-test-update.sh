update=src/data/update
rm $update/*
bake build:client env=local
bake build:update env=local
cp output/Updater/* $update
echo "99.99.99.99" > $update/version.txt
chmod 644 $update/*
chmod +x $update/*.dll
chmod +x $update/*.exe
