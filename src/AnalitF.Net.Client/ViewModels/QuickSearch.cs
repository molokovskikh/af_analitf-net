using System;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class QuickSearch<T> : INotifyPropertyChanged
	{
		private string searchText;
		private bool searchInProgress;
		private Action<T> update;
		private Func<string, T> search;

		public QuickSearch(Func<string, T> search, Action<T> update)
		{
			this.search = search;
			this.update = update;
			var searchTextChanges = this.ObservableForProperty(m => m.SearchText);
			searchTextChanges.Subscribe(_ => OnPropertyChanged("SearchTextVisible"));
			searchTextChanges
				.Throttle(TimeSpan.FromMilliseconds(5000), DispatcherScheduler.Current)
				.Where(o => !String.IsNullOrEmpty(o.Value))
				.Subscribe(_ => SearchText = null);
		}

		public string SearchText
		{
			get { return searchText; }
			set
			{
				//это защита от обнуления запроса в случае если ячейка таблицы
				//потеряла фокус из-за перехода к найденой строке
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
					OnPropertyChanged("SearchText");
			}
		}

		public bool SearchTextVisible
		{
			get { return !String.IsNullOrEmpty(SearchText); }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}