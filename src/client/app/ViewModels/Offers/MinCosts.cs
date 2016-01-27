﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate;
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
				.Do(_ => IsLoading.Value = true)
				.Select(_ => RxQuery(Load))
				.Switch()
				.ObserveOn(UiScheduler)
				.Do(_ => IsLoading.Value = false)
				.ToValue(CloseCancellation);

			CurrentCost
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Subscribe(_ => Update(), CloseCancellation.Token);
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

		protected override void Query()
		{
			if (CurrentCost.Value == null) {
				Offers.Value = new List<Offer>();
				CurrentCatalog.Value = null;
				return;
			}

			var catalogId = CurrentCost.Value.Catalog.Id;
			CurrentCatalog.Value = StatelessSession.Query<Catalog>()
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
