#!/bin/sh

nunit-console-x86 /nologo /run AnalitF.Net.Client.Test.Acceptance.UpdateFixture,AnalitF.Net.Client.Test.Acceptance.StartFixture $* ./src/AnalitF.Net.Test/bin/Debug/AnalitF.Net.Client.Test.dll

rm ./src/client/test/vm/bin/Debug/setup.exe
nunit-console /nologo /run vm.VMFixture $* ./src/client/test/vm/bin/Debug/vm.dll
