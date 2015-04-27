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

namespace AnalitF.Net.Client.ViewModels
{
	public class MnnViewModel : BaseScreen
	{
		public MnnViewModel()
		{
			DisplayName = "Поиск по МНН";
			ShowWithoutOffers = new NotifyValue<bool>();
			SearchBehavior = new SearchBehavior(this);
		}

		public SearchBehavior SearchBehavior { get; set; }
		[Export]
		public NotifyValue<List<Mnn>> Mnns { get; set; }
		public Mnn CurrentMnn { get; set; }
		public NotifyValue<bool> ShowWithoutOffers { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Mnns = SearchBehavior.ActiveSearchTerm.Select(v => (object)v)
				.Merge(ShowWithoutOffers.Select(v => (object)v))
				.Select(_ => RxQuery(s => {
					var query = s.Query<Mnn>();
					if (!ShowWithoutOffers) {
						query = query.Where(m => m.HaveOffers);
					}

					var term = SearchBehavior.ActiveSearchTerm.Value;
					if (!String.IsNullOrEmpty(term)) {
						query = query.Where(m => m.Name.Contains(term));
					}

					return query.OrderBy(m => m.Name).ToList();
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue();
		}

		public void EnterMnn()
		{
			if (CurrentMnn == null)
				return;

			Shell.Navigate(new CatalogViewModel {
				FiltredMnn = CurrentMnn
			});
		}
	}
}