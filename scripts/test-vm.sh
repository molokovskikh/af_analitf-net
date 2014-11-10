#!/bin/sh

set -o errexit
nunit_opts="/nologo /stoponerror /labels"
nunit-console-x86 $nunit_opts /run AnalitF.Net.Client.Test.Acceptance.UpdateFixture,AnalitF.Net.Client.Test.Acceptance.StartFixture $* ./src/client/test/bin/Debug/AnalitF.Net.Client.Test.dll

rm ./src/client/test/vm/bin/Debug/setup.exe || :
nunit-console $nunit_opts /run vm.VMFixture $* ./src/client/test/vm/bin/Debug/vm.dll
