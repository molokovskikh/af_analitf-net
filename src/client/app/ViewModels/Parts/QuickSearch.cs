using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class QuickSearch<T> : ViewAware
	{
		private string searchText;
		private bool searchInProgress;
		private Action<T> update;
		private Func<string, T> search;
		private bool _isEnabled = true;

		public QuickSearch(IScheduler scheduler, Func<string, T> search, Action<T> update)
		{
			this.search = search;
			this.update = update;
			var searchTextChanges = this.ObservableForProperty(m => m.SearchText);
			searchTextChanges.Subscribe(_ => NotifyOfPropertyChange("SearchTextVisible"));
			searchTextChanges
				.Throttle(TimeSpan.FromMilliseconds(5000), scheduler)
				.Where(o => !String.IsNullOrEmpty(o.Value))
				.Subscribe(_ => SearchText = null);
		}

		public bool IsEnabled
		{
			get { return _isEnabled; }
			set
			{
				if (!value)
					SearchText = null;
				_isEnabled = value;
				NotifyOfPropertyChange("IsEnabled");
			}
		}

		public string SearchText
		{
			get { return searchText; }
			set
			{
				if (!IsEnabled)
					return;
				//это защита от обнуления запроса в случае если ячейка таблицы
				//потеряла фокус из-за перехода к найденной строке
				if (searchInProgress)
					return;

				if (String.Equals(searchText, value, StringComparison.CurrentCultureIgnoreCase))
					return;

				searchInProgress = true;
				var notify = false;
				try
				{
					if (!String.IsNullOrEmpty(value)) {
						var result = search(value);
						if (result != null) {
							notify = true;
							searchText = value;
							update(result);
						}
					}
					else {
						notify = true;
						searchText = value;
					}
				}
				finally {
					searchInProgress = false;
				}
				if (notify)
					NotifyOfPropertyChange("SearchText");
			}
		}

		public bool SearchTextVisible
		{
			get { return !String.IsNullOrEmpty(SearchText); }
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			var d = view as DependencyObject;
			if (d != null) {
				var box = d.Descendants<TextBox>().FirstOrDefault();
				if (box == null)
					return;

				var grid = d.Parent().Parent().Children().OfType<DataGrid>().FirstOrDefault();
				if (grid == null)
					return;

				QuickSearchBehavior.AttachSearch(grid, box);
			}
		}
	}
}