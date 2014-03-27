#/bin/sh

iisexpress /systray:false /path:`cygpath -wa src/service/app` | iconv -f cp866 -t utf-8
