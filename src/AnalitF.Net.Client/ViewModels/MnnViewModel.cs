using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class MnnViewModel : BaseScreen
	{
		private bool showWithoutOffers;
		private List<Mnn> mnns;
		private string filterText;

		public MnnViewModel()
		{
			DisplayName = "Поиск по МНН";

			Update();

			this.ObservableForProperty(m => m.ShowWithoutOffers)
				.Subscribe(_ => Update());
		}

		public List<Mnn> Mnns
		{
			get { return mnns; }
			set
			{
				mnns = value;
				RaisePropertyChangedEventImmediately("Mnns");
			}
		}

		public Mnn CurrentMnn { get; set; }

		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				RaisePropertyChangedEventImmediately("ShowWithoutOffers");
			}
		}

		public string SearchText { get; set; }

		public void EnterMnn()
		{
			if (CurrentMnn == null)
				return;

			Shell.ActivateItem(new CatalogViewModel {
				FiltredMnn = CurrentMnn
			});
		}

		public void Update()
		{
			var query = StatelessSession.Query<Mnn>();
			if (!ShowWithoutOffers) {
				query = query.Where(m => m.HaveOffers);
			}

			if (!String.IsNullOrEmpty(filterText)) {
				query = query.Where(m => m.Name.Contains(filterText));
			}

			Mnns = query.OrderBy(m => m.Name).ToList();
		}

		public void Search()
		{
			if (String.IsNullOrEmpty(SearchText))
				return;

			filterText = SearchText;
			Update();
		}
	}
}