#!/bin/sh

lastTag=b`git tag | egrep -o "[0-9]+" | sort -g | tail -n1`
git log $lastTag...head > output/changes.txt
newTag=b$[${lastTag/b/}+1]

/bin/egrep "#[0-9]+" output/changes.txt > output/issues.txt

git tag $newTag head
