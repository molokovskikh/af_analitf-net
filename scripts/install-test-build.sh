#!/bin/bash

rm tmp -rf; mkdir tmp ; cd tmp ; /bin/ls -t //offdc/MMedia/packages/ | head -n1 | xargs -I{} cp //offdc/MMedia/packages/{} ./ ; unzip *.nupkg
find . -iname '*.dll' -or -iname '*.exe' | xargs chmod +x
./tools/*.exe&
