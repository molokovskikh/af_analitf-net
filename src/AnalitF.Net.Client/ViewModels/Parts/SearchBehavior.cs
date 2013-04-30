using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Action = System.Action;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class SearchBehavior
	{
		private Action update;

		public SearchBehavior(CompositeDisposable disposable, IScheduler uiScheduler, IScheduler backgroundScheduler, Action update)
		{
			this.update = update;
			SearchText = new NotifyValue<string>();
			ActiveSearchTerm = new NotifyValue<string>();

			disposable.Add(SearchText.Changes()
				.Throttle(Consts.SearchTimeout, backgroundScheduler)
				.ObserveOn(uiScheduler)
				.Subscribe(_ => Search()));
		}

		public NotifyValue<string> SearchText { get; set; }
		public NotifyValue<string> ActiveSearchTerm { get; set; }

		public IResult ClearSearch()
		{
			if (!String.IsNullOrEmpty(SearchText)) {
				SearchText.Value = "";
				return HandledResult.Handled();
			}

			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm.Value = "";
			SearchText.Value = "";
			update();
			return HandledResult.Handled();
		}

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText.Value) || SearchText.Value.Length < 3)
				return HandledResult.Skip();

			ActiveSearchTerm.Value = SearchText.Value;
			SearchText.Value = "";
			update();
			return HandledResult.Handled();
		}
	}
}