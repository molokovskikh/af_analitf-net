#/bin/sh

iisexpress /systray:false /path:`cygpath -wa src/AnalitF.Net.Service/` | iconv -f cp866 -t utf-8
