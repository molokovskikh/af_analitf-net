using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Common.Tools;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DataMother
	{
		public static void CopyBin(string src, string dst)
		{
			var regex = new Regex(@"(\.dll|\.exe|\.config|\.pdb)$", RegexOptions.IgnoreCase);
			Directory.GetFiles(src).Where(f => regex.IsMatch(f))
				.Each(f => File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true));
		}

		public static string ProjectBin(string name)
		{
			return Path.Combine(GetRoot(), "src", name, "app", "bin", "debug");
		}

		public static string GetRoot([CallerFilePath] string self = null)
		{
			return GetRootDir(Path.GetDirectoryName(self));
		}

		private static string GetRootDir(string dir)
		{
			if (Directory.Exists(Path.Combine(dir, "src")))
				return dir;
			return GetRootDir(Path.Combine(dir, ".."));
		}
	}
}