#!/bin/sh

lastTag=`git tag | tail -n1`
git log $lastTag...head > output/changes.txt
newTag=b$[${lastTag/b/}+1]
git tag $newTag head
