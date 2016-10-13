using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class MinCosts : BaseOfferViewModel
	{
		public MinCosts()
		{
			DisplayName = "Минимальные цены";
			CurrentCost = new NotifyValue<MinCost>();
			Diff = new NotifyValue<int>(7);
			SearchBehavior = new SearchBehavior(this);
			IsLoading = new NotifyValue<bool>(true);
			Persist(Diff, "Diff");
		}

		public SearchBehavior SearchBehavior { get; set; }
		public List<Selectable<Price>> Prices { get; set; }
		public NotifyValue<int> Diff { get; set; }
		public NotifyValue<List<MinCost>> Costs { get; set; }
		public NotifyValue<MinCost> CurrentCost { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }
		public NotifyValue<BitmapImage> Ad { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Ad.Value = Shell.Config.LoadAd("2block.gif");
			Prices = Session.Query<Price>()
				.OrderBy(p => p.Name)
				.Select(p => new Selectable<Price>(p))
				.ToList();
			Costs = Diff.Skip(1).Throttle(Consts.TextInputLoadTimeout, UiScheduler).Select(v => (object)v)
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(SearchBehavior.ActiveSearchTerm)
				.Merge(DbReloadToken)
				.Do(_ => IsLoading.Value = true)
				.Select(_ => RxQuery(Load))
				.Switch()
				.Do(_ => IsLoading.Value = false)
				.ToValue(CloseCancellation);

			CurrentCost
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Merge(DbReloadToken)
				.Subscribe(_ => UpdateAsync(), CloseCancellation.Token);
		}

		private List<MinCost> Load(IStatelessSession session)
		{
			var factor = Diff.Value;
			var query = session.Query<MinCost>()
				.Fetch(c => c.Catalog)
				.ThenFetch(c => c.Name)
				.Where(c => c.Diff > factor);
			query = Util.Filter(query, c => c.Price.Id, Prices);
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term)) {
				query = query.Where(c => c.Catalog.Name.Name.Contains(term) || c.Catalog.Form.Contains(term));
			}
			return query.OrderBy(c => c.Catalog.Name.Name)
				.ThenBy(c => c.Catalog.Form)
				.Fetch(c => c.Catalog)
				.ThenFetch(c => c.Name)
				.ToList();
		}

		private void UpdateAsync()
		{
			if (CurrentCost.Value == null) {
				Offers.Value = new List<Offer>();
				CurrentCatalog.Value = null;
				return;
			}

			var productId = CurrentCost.Value.ProductId;
			Env.RxQuery(s => s.Query<Offer>()
					.Fetch(o => o.Price)
					.Where(o => o.ProductId == productId)
					.ToList()
					.OrderBy(o => o.ResultCost)
					.ToList())
				.Subscribe(UpdateOffers, CloseCancellation.Token);
		}
	}
}
