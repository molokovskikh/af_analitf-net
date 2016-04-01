using System;
using System.IO;
using System.Linq;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;

namespace AnalitF.Net.Client.Helpers
{
	public class FileHelper2
	{
		public static void InitFile(string filename)
		{
			FileHelper.CreateDirectoryRecursive(Path.GetDirectoryName(filename));
			File.WriteAllText(filename, "");
		}

		/// <summary>
		/// Directory.Delete - не удалит директорию если на эту директорию есть открытые указатели
		/// а только отметит ее к удалению
		/// Если есть код в духе
		/// Directory.Delete
		/// Directory.CreateDirectory
		/// то это ошибка ждущая того что бы случится тк Directory.Delete отметит но не удалит
		/// Directory.CreateDirectory - проверит что есть и ничего делать не станет
		/// а последующий код будет предполагать что директория существует но она может быть удалена
		/// перед любой операцией
		///
		/// не выбрасывает исключение если файл не существует по аналогии с File.Delete
		/// </summary>
		public static void DeleteDir(string dir, bool recursive = true)
		{
			if (!Directory.Exists(dir))
				return;
			Directory.Delete(dir, recursive);
			var timeout = 5.Minute();
			WaitHelper.WaitOrFail(timeout, () => !Directory.Exists(dir), $"Не удалось удалить директорию {dir} за {timeout}");
		}
	}
}