using System;
using System.IO;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Client.Helpers
{
	public class FileHelper2
	{
		public static void InitFile(string filename)
		{
			FileHelper.CreateDirectoryRecursive(Path.GetDirectoryName(filename));
			File.WriteAllText(filename, "");
		}
	}
}