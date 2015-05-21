using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderRejectDetails : BaseOfferViewModel
	{
		private uint id;

		public OrderRejectDetails(uint id)
		{
			this.id = id;
			Doc = new NotifyValue<OrderReject>();
			Lines = new NotifyValue<List<OrderRejectLine>>();
			CurrentLine = new NotifyValue<OrderRejectLine>();
			DisplayName = "Отказ";
		}

		public ProductInfo ProductInfo { get; set; }
		public NotifyValue<OrderRejectLine> CurrentLine { get; set; }
		public NotifyValue<List<OrderRejectLine>> Lines { get; set; }
		public NotifyValue<OrderReject> Doc { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(this, CurrentOffer);
			Doc.Value = Session.Query<OrderReject>().First(r => r.DownloadId == id);
			Lines.Value = Doc.Value.Lines.OrderBy(l => l.Product).ToList();

			CurrentLine
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Select(v => RxQuery(s => {
						if (CurrentLine.Value == null)
							return Enumerable.Empty<Offer>().ToList();

						var productId = CurrentLine.Value.ProductId;
						if (productId != null) {
							var offers = s.Query<Offer>().Where(o => o.ProductId == productId)
								.Fetch(o => o.Price)
								.ToList();
							return offers.OrderBy(c => c.ResultCost).ToList();
						}
						else {
							return SearchOfferViewModel.QueryByFullText(s, CurrentLine.Value.Product);
						}
					}))
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

		public void EnterLine()
		{
			if (CurrentLine.Value == null)
				return;
			if (CurrentLine.Value.CatalogId == null)
				Shell.Navigate(new SearchOfferViewModel(CurrentLine.Value.Product));
			else
				Shell.Navigate(new CatalogOfferViewModel((long)CurrentLine.Value.CatalogId));
		}
	}
}