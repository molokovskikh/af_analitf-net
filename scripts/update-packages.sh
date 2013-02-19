cd packages
../scripts/clean-packages.sh
sed 's/ -Version.\+//' packages.txt | xargs -L1 nuget install
../scripts/fix-packages.sh > packages.update.txt
mv packages{.update,}.txt
cd ..
bake packages
