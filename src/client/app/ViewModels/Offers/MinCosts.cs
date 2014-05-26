using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using NHibernate.Mapping;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class MinCosts : BaseOfferViewModel
	{
		public MinCosts()
		{
			DisplayName = "Минимальные цены";
			CurrentCost = new NotifyValue<MinCost>();
			Diff = new NotifyValue<int>(7);
			SearchBehavior = new SearchBehavior(this, callUpdate: false);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public List<Selectable<Price>> Prices { get; set; }
		public NotifyValue<int> Diff { get; set; }
		public NotifyValue<List<MinCost>> Costs { get; set; }
		public NotifyValue<MinCost> CurrentCost { get; set; }

		public IResult Search()
		{
			return SearchBehavior.Search();
		}

		public IResult ClearSearch()
		{
			return SearchBehavior.ClearSearch();
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Prices = Session.Query<Price>()
				.OrderBy(p => p.Name)
				.Select(p => new Selectable<Price>(p))
				.ToList();
			Costs = Diff.Throttle(Consts.ScrollLoadTimeout, UiScheduler).Select(v => (object)v)
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(SearchBehavior.ActiveSearchTerm)
				.Select(_ => Load())
				.ToValue(_ => Load(), CloseCancellation);

			CurrentCost
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Subscribe(_ => Query(), CloseCancellation.Token);

			Costs.Value = Load();
		}

		private List<MinCost> Load()
		{
			var factor = 1 + Diff.Value / 100m;
			var query = Session.Query<MinCost>()
				.Fetch(c => c.Catalog)
				.ThenFetch(c => c.Name)
				.Where(c => c.NextCost / c.Cost > factor);
			query = Util.Filter(query, c => c.Price.Id, Prices);
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term)) {
				query = query.Where(c => c.Catalog.Name.Name.Contains(term) || c.Catalog.Form.Contains(term));
			}
			return query.OrderBy(c => c.Catalog.Name.Name)
				.ThenBy(c => c.Catalog.Form)
				.ToList();
		}

		protected override void Query()
		{
			if (CurrentCost.Value == null) {
				Offers.Value = new List<Offer>();
				CurrentCatalog = null;
				return;
			}

			var catalogId = CurrentCost.Value.Catalog.Id;
			CurrentCatalog = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(n => n.Mnn)
				.First(c => c.Id == catalogId);

			var productId = CurrentCost.Value.ProductId;
			Offers.Value = StatelessSession.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.ProductId == productId)
				.ToList()
				.OrderBy(o => o.ResultCost)
				.ToList();
		}
	}
}