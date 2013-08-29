using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class MnnViewModel : BaseScreen
	{
		public MnnViewModel()
		{
			DisplayName = "Поиск по МНН";
			Readonly = true;

			ShowWithoutOffers = new NotifyValue<bool>();
			Mnns = new NotifyValue<List<Mnn>>(
				new List<Mnn>(),
				() => {
					var query = StatelessSession.Query<Mnn>();
					if (!ShowWithoutOffers) {
						query = query.Where(m => m.HaveOffers);
					}

					var term = SearchBehavior.ActiveSearchTerm.Value;
					if (!String.IsNullOrEmpty(term)) {
						query = query.Where(m => m.Name.Contains(term));
					}

					return query.OrderBy(m => m.Name).ToList();
				},
				ShowWithoutOffers);

			SearchBehavior = new SearchBehavior(OnCloseDisposable, UiScheduler, Scheduler, Update);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<List<Mnn>> Mnns { get; set; }
		public Mnn CurrentMnn { get; set; }
		public NotifyValue<bool> ShowWithoutOffers { get; set; }

		public void EnterMnn()
		{
			if (CurrentMnn == null)
				return;

			Shell.Navigate(new CatalogViewModel {
				FiltredMnn = CurrentMnn
			});
		}

		public override void Update()
		{
			Mnns.Recalculate();
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