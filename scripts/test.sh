#!/bin/sh

set -o errexit
mkdir output || :
mkdir output/test || :

mkdir src/client/test/bin/Debug/var || :
mkdir src/client/test/bin/Debug/var/service || :
mkdir src/client/test/bin/Debug/var/service/localexport || :

chmod o+w src/client/test/bin/Debug/var/service/localexport
nunit3-console --labels=all --stoponerror --verbose --full $* ./src/client/test/bin/Debug/AnalitF.Net.Client.Test.dll
mv TestResult.xml output/test/AnalitF.Net.Client.Test.dll.xml

mkdir src/service/test/bin/Debug/var || :
mkdir src/service/test/bin/Debug/var/localexport || :
chmod o+w src/service/test/bin/Debug/var/localexport
nunit3-console /nologo /stoponerror /labels $* ./src/service/test/bin/Debug/AnalitF.Net.Service.Test.dll
mv TestResult.xml output/test/AnalitF.Net.Service.Test.dll.xml
