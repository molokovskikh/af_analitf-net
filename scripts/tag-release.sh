#!/bin/sh

lastTag=`git tag |  sort -g | tail -n1`
git log $lastTag...head > output/changes.txt
parts=(`echo $lastTag | egrep -o [0-9]+`)
major=${parts[0]}
minor=${parts[1]}
patch=${parts[2]}
revision=${parts[3]}
revision=$[$revision+1]
if [ "$1" == "major" ]
then
	major=$[$major+1]
	minor=0
	patch=0
fi
if [ "$1" == "minor" ]
then
	minor=$[$minor+1]
	patch=0
fi
if [ "$1" == "patch" ]
then
	patch=$[$patch+1]
fi
newTag=v$major.$minor.$patch.$revision

/bin/egrep "#[0-9]+" output/changes.txt > output/issues.txt

git tag $newTag head
