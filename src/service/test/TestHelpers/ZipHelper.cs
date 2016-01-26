using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;

namespace AnalitF.Net.Service.Test.TestHelpers
{
	public class ZipHelper
	{
		public static List<string> lsZip(byte[] buffer)
		{
			using(var zip = ZipFile.Read(new MemoryStream(buffer)))
				return zip.Select(z => z.FileName).ToList();
		}

		public static List<string> lsZip(string file)
		{
			using(var zip = ZipFile.Read(file))
				return zip.Select(z => z.FileName).ToList();
		}
	}
}