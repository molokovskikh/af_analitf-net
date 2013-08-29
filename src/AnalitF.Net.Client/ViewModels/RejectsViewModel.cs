using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class RejectsViewModel : BaseScreen, IPrintable
	{
		public RejectsViewModel()
		{
			Readonly = true;
			DisplayName = "Забракованные препараты";
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3));
			End = new NotifyValue<DateTime>(DateTime.Today);
			ShowCauseReason = new NotifyValue<bool>();
			CurrentReject = new NotifyValue<Reject>();
			CanMark = new NotifyValue<bool>(() => CurrentReject.Value != null, CurrentReject);

			Rejects = new NotifyValue<List<Reject>>(new List<Reject>(),
				() => {
					var begin = Begin.Value;
					var end = End.Value.AddDays(1);
					return StatelessSession.Query<Reject>()
						.Where(r => r.LetterDate >= begin && r.LetterDate < end)
						.OrderBy(r => r.LetterDate)
						.ToList();
				}, End, Begin);
			WatchForUpdate(CurrentReject);
		}

		[Export]
		public NotifyValue<List<Reject>> Rejects { get; set; }

		public NotifyValue<Reject> CurrentReject { get; set; }

		public NotifyValue<DateTime> Begin { get; set; }

		public NotifyValue<DateTime> End { get; set; }

		public NotifyValue<bool> ShowCauseReason { get; set; }

		public NotifyValue<bool> CanMark { get; set; }

		public override void Update()
		{
			Rejects.Recalculate();
		}

		public void Mark()
		{
			if (!CanMark)
				return;

			CurrentReject.Value.Marked = !CurrentReject.Value.Marked;
		}

		public void ClearMarks()
		{
			StatelessSession
				.CreateSQLQuery("update rejects set Marked = 0")
				.ExecuteUpdate();
			Rejects.Value.Each(r => r.Marked = false);
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			IList<Reject> toPrint = StatelessSession.Query<Reject>().Where(r => r.Marked).ToList();
			if (toPrint.Count == 0) {
				toPrint = Rejects.Value;
			}
			var doc = new RejectsDocument(toPrint, ShowCauseReason).Build();
			return new PrintResult(DisplayName, doc);
		}
	}
}