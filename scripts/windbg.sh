windbg -c ".loadby sos clr" -p `ps -W | /bin/grep AnalitF.Net.Client | awk '{print $1}'`
