VBoxManage controlvm win7 poweroff
VBoxManage unregistervm win7 --delete
vboxmanage clonevm "Чистая Windows 7" --name win7
vboxmanage registervm "$USERPROFILE/VirtualBox VMs/win7/win7.vbox"
vboxmanage startvm win7
