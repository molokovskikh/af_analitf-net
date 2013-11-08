net use x: \\vboxsrv\analitf-net
elevate -k net user test_priv 123 /add
copy /Y x:\elevate.exe .\
copy /Y x:\output\setup\setup.exe .\
elevate -k copy /Y c:\users\test\setup.exe c:\users\test_priv\setup.exe
runas /profile /user:test_priv c:\users\test_priv\setup.exe
