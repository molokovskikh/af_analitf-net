vboxmanage sharedfolder add win7 --name analitf-net --hostpath `cygpath -wa .`
vboxmanage guestcontrol win7 copyto `cygpath -wa run.cmd` 'C:\users\test\' --username=test --password=123
vboxmanage guestcontrol win7 copyto `cygpath -wa elevate.exe` 'C:\users\test\' --username=test --password=123
vboxmanage guestcontrol win7 exec 'C:\users\test\run.cmd' --username=test --password=123 --wait-exit --wait-stdout --wait-stderr | iconv -f cp866 -t utf-8
