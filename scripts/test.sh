#!/bin/sh

nunit-console-x86 /nologo $* ./src/AnalitF.Net.Test/bin/Debug/AnalitF.Net.Client.Test.dll
nunit-console-x86 /nologo $* ./src/AnalitF.Net.Service.Test/bin/Debug/AnalitF.Net.Service.Test.dll
