pass=12345678
bake build:client env=test
vboxmanage guestcontrol win7 copyto `cygpath -wa output/setup/setup.exe` 'C:\users\test\' --username test --password $pass
vboxmanage guestcontrol win7 exec --verbose --timeout 1800000 --image 'C:\users\test\setup.exe' --username test --password $pass --wait-exit --wait-stdout -- -q | iconv -f cp1251 -t utf-8
vboxmanage guestcontrol win7 exec --verbose --image 'C:\users\test\appdata\local\аналитфармация\analitf.net.client.exe' --username test --password $pass --wait-exit --wait-stdout -- --quiet start-check | iconv -f cp1251 -t utf-8
