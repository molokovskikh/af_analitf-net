windbg -c ".loadby sos clr;!dumpheap -type AnalitF.Net.Client.ViewModels" -p `ps -W | /bin/grep AnalitF.Net.Client | head -n1 | awk '{print $1}'`
