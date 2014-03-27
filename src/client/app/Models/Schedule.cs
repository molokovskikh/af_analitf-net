using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq.Observαble;
using System.Windows.Forms.VisualStyles;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class Schedule
	{
		public Schedule()
		{
		}

		public Schedule(TimeSpan updateAt)
		{
			UpdateAt = updateAt;
		}

		public virtual uint Id { get; set; }
		public virtual TimeSpan UpdateAt { get; set; }

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
			return Win32.GetThreadWindows(threadId).Count(p => Win32.IsWindowVisible(p)) <= 1;
		}
	}
}