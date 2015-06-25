using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using log4net;

namespace AnalitF.Net.Client.Models
{
	public class Schedule
	{
		private static ILog log = LogManager.GetLogger(typeof(Schedule));

		public Schedule()
		{
		}

		public Schedule(TimeSpan updateAt)
		{
			UpdateAt = updateAt;
		}

		public virtual uint Id { get; set; }
		public virtual TimeSpan UpdateAt { get; set; }

		public override string ToString()
		{
			return UpdateAt.ToString();
		}

		public static bool IsOutdate(IList<Schedule> shedules, DateTime lastUpdate)
		{
			if (shedules.Count == 0)
				return false;
			var expectedUpdate = shedules.Select(t => SystemTime.Today().Add(t.UpdateAt))
				.Where(t => t < SystemTime.Now())
				.MaxOrDefault();
			return expectedUpdate > lastUpdate || (SystemTime.Now() - lastUpdate) >= TimeSpan.FromDays(1);
		}

		public static bool CanStartUpdate()
		{
			var threadId = Win32.GetCurrentThreadId();
			//если мы запустили приложение а обновление по расписанию было пропущено
			//то мы должны показать диалог и запустить обновление
			//при запуске есть короткий промежуток когда видно и главное окно и окно заствавки
			//при вычислении открытых окон нужно игнорировать заставку
			var windows = Win32.GetThreadWindows(threadId)
				.Where(x => Win32.IsWindowVisible(x) && GetClass(x) != "SplashScreen");
			var count = windows.Count();
			if (count > 1 && log.IsDebugEnabled) {
				foreach (var threadWindow in windows) {
					log.Debug(DumpWindow(threadWindow));
				}
			}
			return count <= 1;
		}

		public static string DumpWindow(IntPtr threadWindow)
		{
			var text = new StringBuilder(Win32.GetWindowTextLength(threadWindow) + 1);
			Win32.GetWindowText(threadWindow, text, text.Capacity);
			var clazz = GetClass(threadWindow);
			return String.Format("hwnd = {0}, class = {2}, text = {1}", threadWindow, text, clazz);
		}

		private static string GetClass(IntPtr threadWindow)
		{
			var clazz = new StringBuilder(256);
			Win32.GetClassName(threadWindow, clazz, clazz.Capacity);
			return clazz.ToString();
		}
	}
}