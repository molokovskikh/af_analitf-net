using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
				.Do(_ => IsLoading.Value = true)
				.Select(_ => RxQuery(s => {
					var begin = Begin.Value;
					var end = End.Value.AddDays(1);
					var result = s.Query<Reject>()
						.Where(r => r.LetterDate >= begin && r.LetterDate < end)
						.OrderBy(r => r.LetterDate)
						.ToList();
					return result;
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.Do(_ => IsLoading.Value = false)
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
			Env.Query(s => s.CreateSQLQuery("update rejects set Marked = 0").ExecuteUpdate()).LogResult();
		}

		public bool CanPrint => User.CanPrint<RejectsDocument>();

		public PrintResult Print()
		{
			IList<Reject> toPrint = Env
				.Query(s => s.Query<Reject>().Where(r => r.Marked)
					.OrderBy(r => r.LetterDate).ToList()).Result;
			if (toPrint.Count == 0) {
				toPrint = GetItemsFromView<Reject>("Rejects") ?? Rejects.Value;
			}
			else {
				toPrint = ApplySort("Rejects", toPrint);
			}
			return new PrintResult(DisplayName, new RejectsDocument(toPrint, ShowCauseReason));
		}

		private Reject[] ApplySort(string name, IList<Reject> items)
		{
			var array = items.ToArray();
			var view = GetView();
			if (view == null)
				return array;
			var grid = ((FrameworkElement)view).Descendants<DataGrid>().First(g => g.Name == name);
			var desc = grid.Items.SortDescriptions.FirstOrDefault();
			if (String.IsNullOrEmpty(desc.PropertyName))
				return array;
			var direction = desc.Direction == ListSortDirection.Ascending ? SortDirection.Asc : SortDirection.Desc;
			Array.Sort(array, new PropertyComparer(direction, desc.PropertyName));
			return array;
		}
	}
}