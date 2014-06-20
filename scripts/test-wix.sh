#!/bin/sh

set -o errexit
dir=/cygdrive/c/Users/kvasov/AppData/Local/АналитФАРМАЦИЯ/

function test_uninstall {
	./scripts/uninstall.sh

	./scripts/wix.sh
	./output/setup/Setup.exe /verbose /install /quiet /log setup.install.log
	./output/setup/Setup.exe /verbose /uninstall /quiet /log setup.uninstall.log
	if [ -d $dir ]; then
		echo "после удаления осталась установочная директория $dir"
		exit 1
	fi
	echo "тест удаления успешно завершен"
}

function test_update {
	./scripts/uninstall.sh

	./scripts/wix.sh --version=1.1
	./output/setup/Setup.exe /verbose /install /quiet /log setup.install.log
	/cygdrive/c/Users/kvasov/AppData/Local/АналитФАРМАЦИЯ/AnalitF.Net.Client.exe --quiet start-check
	if [ ! -d $dir/data ]; then
		echo "после запуска приложения база данных $dir/data не была создана"
		exit 1
	fi

	./scripts/wix.sh --version=1.2
	./output/setup/Setup.exe /verbose /install /quiet /log setup.install.log
	if [ ! -d $dir ]; then
		echo "в результате обновления база данных $dir/data была удалена"
		exit 1
	fi
	echo "тест обновления успешно завершен"
}

if [[ ${BASH_SOURCE[0]} == "$0" ]]; then
	test_uninstall
	test_update
fi
