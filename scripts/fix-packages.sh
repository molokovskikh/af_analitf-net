ex="([a-zA-Z\.0-9\_\-]+) -Version (.+)"
current=`ls`
version="((([0-9]+\.){1,3})[0-9]+)"
updated=""
while read line
do
	if [[ $line =~ $ex ]]
	then
		small=${BASH_REMATCH[1]}
		name=${BASH_REMATCH[1]}.$version
		if [[ $current =~ $name ]]
		then
			echo "$small -Version ${BASH_REMATCH[1]}"
		fi
	fi
done <packages.txt
echo $update
