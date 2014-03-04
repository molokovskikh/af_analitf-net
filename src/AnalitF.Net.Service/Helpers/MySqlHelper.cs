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
						var value = item[i];
						if (value == null)
							file.Write(@"\N");
						else if (value is DateTime)
							file.Write(((DateTime)value).ToString(MySqlConsts.MySQLLongDateTimeFormat));
						else if (value is string)
							file.Write(MySql.Data.MySqlClient.MySqlHelper.EscapeString((string)value).Replace("\n", "\\\n"));
						else
							file.Write(value);
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