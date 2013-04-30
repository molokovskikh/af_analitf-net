using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class MnnViewModel : BaseScreen
	{
		private bool showWithoutOffers;

		public MnnViewModel()
		{
			DisplayName = "Поиск по МНН";

			Mnns = new NotifyValue<List<Mnn>>();

			this.ObservableForProperty(m => m.ShowWithoutOffers)
				.Subscribe(_ => Update());

			SearchBehavior = new SearchBehavior(OnCloseDisposable, UiScheduler, Scheduler, Update);
		}

		public SearchBehavior SearchBehavior { get; set; }

		public NotifyValue<List<Mnn>> Mnns { get; set; }
		public Mnn CurrentMnn { get; set; }

		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				NotifyOfPropertyChange("ShowWithoutOffers");
			}
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Update();
		}

		public void EnterMnn()
		{
			if (CurrentMnn == null)
				return;

			Shell.Navigate(new CatalogViewModel {
				FiltredMnn = CurrentMnn
			});
		}

		public void Update()
		{
			var query = StatelessSession.Query<Mnn>();
			if (!ShowWithoutOffers) {
				query = query.Where(m => m.HaveOffers);
			}

			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term)) {
				query = query.Where(m => m.Name.Contains(term));
			}

			Mnns.Value = query.OrderBy(m => m.Name).ToList();
		}

		public IResult Search()
		{
			return SearchBehavior.Search();
		}

		public IResult ClearSearch()
		{
			return SearchBehavior.ClearSearch();
		}
	}
}