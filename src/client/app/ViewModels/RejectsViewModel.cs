using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class RejectsViewModel : BaseScreen, IPrintable
	{
		public RejectsViewModel()
		{
			DisplayName = "Забракованные препараты";
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3));
			End = new NotifyValue<DateTime>(DateTime.Today);
			ShowCauseReason = new NotifyValue<bool>();
			CurrentReject = new NotifyValue<Reject>();
			CanMark = CurrentReject.Select(r => r != null).ToValue();
			IsLoading = new NotifyValue<bool>(true);
			QuickSearch = new QuickSearch<Reject>(UiScheduler,
				t => Rejects.Value.FirstOrDefault(o => o.Product.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0),
				CurrentReject);
			QuickSearch.RemapChars = true;

			WatchForUpdate(CurrentReject);
		}

		[Export]
		public NotifyValue<List<Reject>> Rejects { get; set; }

		public NotifyValue<Reject> CurrentReject { get; set; }

		public NotifyValue<DateTime> Begin { get; set; }

		public NotifyValue<DateTime> End { get; set; }

		public NotifyValue<bool> ShowCauseReason { get; set; }

		public NotifyValue<bool> CanMark { get; set; }

		public NotifyValue<bool> IsLoading { get; set; }

		public QuickSearch<Reject> QuickSearch { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Rejects = Begin.Concat(End)
				.Select(_ => RxQuery(s => {
					IsLoading.Value = true;
					var begin = Begin.Value;
					var end = End.Value.AddDays(1);
					var result = StatelessSession.Query<Reject>()
						.Where(r => r.LetterDate >= begin && r.LetterDate < end)
						.OrderBy(r => r.LetterDate)
						.ToList();
					IsLoading.Value = false;
					return result;
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
		}

		public void Mark()
		{
			if (!CanMark)
				return;

			CurrentReject.Value.Marked = !CurrentReject.Value.Marked;
		}

		public void ClearMarks()
		{
			Rejects.Value.Each(r => r.Marked = false);
			StatelessSession
				.CreateSQLQuery("update rejects set Marked = 0")
				.ExecuteUpdate();
		}

		public bool CanPrint
		{
			get { return User.CanPrint<RejectsDocument>(); }
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