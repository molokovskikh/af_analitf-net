using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class MnnViewModel : BaseScreen
	{
		private bool showWithoutOffers;
		private List<Mnn> mnns;

		public MnnViewModel()
		{
			DisplayName = "Поиск по МНН";

			SearchText = new NotifyValue<string>();
			ActiveSearchTerm = new NotifyValue<string>();

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
				NotifyOfPropertyChange("Mnns");
			}
		}

		public Mnn CurrentMnn { get; set; }

		public NotifyValue<string> SearchText { get; set; }
		public NotifyValue<string> ActiveSearchTerm { get; set; }

		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				NotifyOfPropertyChange("ShowWithoutOffers");
			}
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

			if (!String.IsNullOrEmpty(ActiveSearchTerm.Value)) {
				query = query.Where(m => m.Name.Contains(ActiveSearchTerm.Value));
			}

			Mnns = query.OrderBy(m => m.Name).ToList();
		}

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText.Value) || SearchText.Value.Length < 3)
				return HandledResult.Skip();

			ActiveSearchTerm.Value = SearchText.Value;
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public IResult ClearSearch()
		{
			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm.Value = "";
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}
	}
}