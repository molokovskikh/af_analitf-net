using System.Collections.Generic;
using System.Linq;
using Ionic.Zip;

namespace AnalitF.Net.Service.Test.TestHelpers
{
	public class ZipHelper
	{
		public static List<string> lsZip(string file)
		{
			using(var zip = ZipFile.Read(file)) {
				return zip.Select(z => z.FileName).ToList();
			}
		}
	}
}