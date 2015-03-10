using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
		public SearchOfferViewModel()
		{
			DisplayName = "Поиск в прайс-листах";
			NeedToCalculateDiff = true;
			NavigateOnShowCatalog = true;

			OnlyBase = new NotifyValue<bool>();

			if (Session != null) {
				Prices = Session.Query<Price>()
					.OrderBy(p => p.Name)
					.Select(p => new Selectable<Price>(p))
					.ToList();
			}
			else {
				Prices = new List<Selectable<Price>>();
			}
			Settings.Subscribe(_ => {
				Offers.Value = SortOffers(Offers);
			});
			SearchBehavior = new SearchBehavior(this);
			IsLoading = new NotifyValue<bool>();
		}

		public SearchBehavior SearchBehavior { get; set; }
		public List<Selectable<Price>> Prices { get; set; }
		public NotifyValue<bool> OnlyBase { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(s => s.Query<Producer>()
					.OrderBy(p => p.Name)
					.ToList())
				.ObserveOn(UiScheduler)
				.Subscribe(Producers);

			SearchBehavior.ActiveSearchTerm.Cast<object>()
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(OnlyBase.Changed())
				.Merge(CurrentProducer.Changed())
				.Select(v => {
					var term = SearchBehavior.ActiveSearchTerm.Value;
					if (String.IsNullOrEmpty(term))
						return Observable.Return(new List<Offer>());

					return RxQuery(s => {
						IsLoading.Value = true;
						var query = StatelessSession.Query<Offer>();
						query = Util.ContainsAny(query, o => o.ProductSynonym,
							term.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
						query = Util.Filter(query, o => o.Price.Id, Prices);

						var producer = CurrentProducer.Value;
						if (producer != null && producer.Id > 0) {
							var id = producer.Id;
							query = query.Where(o => o.ProducerId == id);
						}

						if (OnlyBase)
							query = query.Where(o => o.Price.BasePrice);

						var result = SortOffers(query.Fetch(o => o.Price).ToList());
						IsLoading.Value = false;
						return result;
					});
				})
				.Switch()
				.ObserveOn(UiScheduler)
				//будь бдителен CalculateRetailCost и LoadOrderItems может вызвать обращение к базе если данные еще не загружены
				//тк синхронизация не производится загрузка должна выполняться в основной нитке
				.Do(v => {
					LoadOrderItems(v);
					CalculateRetailCost(v);
				})
				.Subscribe(Offers, CloseCancellation.Token);
		}

		private List<Offer> SortOffers(List<Offer> offers)
		{
			if (Settings.Value.GroupByProduct) {
				return SortByMinCostInGroup(offers, o => o.ProductId);
			}
			else {
				return SortByMinCostInGroup(offers, o => o.CatalogId);
			}
		}
	}
}