using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using ReactiveUI;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class QuickSearch<T> : ViewAware
	{
		public const string RusKeys = "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
		public const string EngKeys = "f,dult`;pbqrkvyjghcnea[wxio]sm.zF<DULT~:PBQRKVYJGHCNEA{WXIO}SM\">Z";
		public static Dictionary<char, char> CharMap = new Dictionary<char, char>();

		private string searchText;
		public bool searchInProgress;
		private Action<T> update;
		private Func<string, T> search;
		private bool _isEnabled = true;

		public bool RemapChars;

		static QuickSearch()
		{
			for(var i = 0; i < EngKeys.Length; i++) {
				CharMap.Add(EngKeys[i], RusKeys[i]);
			}
		}

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

				var text = value;
				if (!String.IsNullOrEmpty(text) && RemapChars)
					text = new string(text.Select(c => CharMap.GetValueOrDefault(c, c)).ToArray());
				if (String.Equals(searchText, text, StringComparison.CurrentCultureIgnoreCase))
					return;

				searchInProgress = true;
				var notify = false;
				try
				{
					if (!String.IsNullOrEmpty(text)) {
						var result = search(text);
						if (result != null) {
							notify = true;
							searchText = text;
							update(result);
						}
					}
					else {
						notify = true;
						searchText = text;
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

				var contentControl = d.Parent();
				var grid = contentControl.GetValue(QuickSearchBehavior.GridRef) as DataGrid
					?? contentControl.Parent().Children().OfType<DataGrid>().FirstOrDefault();
				if (grid == null)
					return;

				QuickSearchBehavior.AttachSearch(grid, box);
			}
		}
	}
}