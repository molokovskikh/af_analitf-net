for guid in `grep "АналитФАРМАЦИЯ"  /proc/registry64/HKEY_CURRENT_USER/Software/Microsoft/Windows/CurrentVersion/Uninstall/ -Rl`; do
	guid=`dirname $guid`
	guid=`basename $guid`
	cmd=`cat /proc/registry64/HKEY_CURRENT_USER/Software/Microsoft/Windows/CurrentVersion/Uninstall/$guid/UninstallString`
	echo $cmd
	cmd.exe /C "$cmd /quiet"
done
