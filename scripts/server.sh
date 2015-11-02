#/bin/sh

iisexpress /systray:false /path:`cygpath -wa src/service/app` | iconv -f cp1251 -t utf-8
