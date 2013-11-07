using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Common.MySql;

namespace AnalitF.Net.Service.Helpers
{
	public class MySqlHelper
	{
		public static void Export(IEnumerable<object[]> exportData, TextWriter file)
		{
			var originCulture = Thread.CurrentThread.CurrentCulture;
			try {
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				foreach (var item in exportData) {
					for (var i = 0; i < item.Length; i++) {
						if (item[i] == null)
							file.Write(@"\N");
						else if (item[i] is DateTime)
							file.Write(((DateTime)item[i]).ToString(MySqlConsts.MySQLLongDateTimeFormat));
						else
							file.Write(item[i]);
						file.Write("\t");
					}
					file.WriteLine();
				}
			}
			finally {
				Thread.CurrentThread.CurrentCulture = originCulture;
			}
		}
	}
}