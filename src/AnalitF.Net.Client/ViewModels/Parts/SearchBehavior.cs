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
	//очередное безумее будь бдителен
	//BaseNotify не используется но если wpf binding используется для класса без INotifyPropertyChanged
	//этот объект попадет в глобальную таблицу внутри wpf и это приведет к утечки памяти
	public class SearchBehavior : BaseNotify
	{
		private BaseScreen screen;

		public SearchBehavior(BaseScreen screen)
		{
			SearchText = new NotifyValue<string>();
			ActiveSearchTerm = new NotifyValue<string>();
			this.screen = screen;

			screen.OnCloseDisposable.Add(SearchText.Changed()
				.Throttle(Consts.SearchTimeout, screen.Scheduler)
				.ObserveOn(screen.UiScheduler)
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
			screen.Update();
			return HandledResult.Handled();
		}

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText.Value) || SearchText.Value.Length < 3)
				return HandledResult.Skip();

			ActiveSearchTerm.Value = SearchText.Value;
			SearchText.Value = "";
			screen.Update();
			return HandledResult.Handled();
		}
	}
}