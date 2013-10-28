using System;
using System.IO;
using System.Linq;

namespace AnalitF.Net.Client.Helpers
{
	public class FileHelper2
	{
		public static string Uniq(string filename)
		{
			if (!File.Exists(filename))
				return filename;

			var dir = Path.GetDirectoryName(filename);
			var posibleNames = Enumerable.Range(0, 9).Select(i => Path.Combine(dir, String.Format("{0}_{1}{2}",
				Path.GetFileNameWithoutExtension(filename),
				i,
				Path.GetExtension(filename))));
			return posibleNames.Where(n => !File.Exists(n))
				.DefaultIfEmpty(posibleNames.First())
				.First();
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