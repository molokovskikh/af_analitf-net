#!/bin/sh

mkdir output
mkdir output/test
nunit-console-x86 /nologo $* ./src/AnalitF.Net.Test/bin/Debug/AnalitF.Net.Client.Test.dll
mv TestResult.xml output/test/AnalitF.Net.Client.Test.dll.xml
nunit-console-x86 /nologo $* ./src/AnalitF.Net.Service.Test/bin/Debug/AnalitF.Net.Service.Test.dll
mv TestResult.xml output/test/AnalitF.Net.Service.Test.dll.xml
