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

		public static void CopyDir(string src, string dst)
		{
			if (!Directory.Exists(dst)) {
				Directory.CreateDirectory(dst);
			}

			foreach (var file in Directory.GetFiles(src)) {
				File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
			}

			foreach (var dir in Directory.GetDirectories(src)) {
				CopyDir(dir, Path.Combine(dst, Path.GetFileName(dir)));
			}
		}
	}
}