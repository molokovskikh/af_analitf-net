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

		//для восстановления состояния
		public OrderRejectDetails(long id)
			: this((uint)id)
		{
		}

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
		public string DisplaySupplierName { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(this, CurrentOffer);
			Doc.Value = Session.Query<OrderReject>().First(r => r.DownloadId == id);
			if (Doc.Value.Supplier == null)
				DisplaySupplierName = Session.Query<Waybill>().First(r => r.Id == id).UserSupplierName;
			else
				DisplaySupplierName = Doc.Value.Supplier.FullName;

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
							var offers = SearchOfferViewModel.QueryByFullText(s, CurrentLine.Value.Product);
							//выделяем цветом предложения которые относятся к одному товару
							uint lastCatalogId = 0;
							bool lastGroup = true;
							foreach (var offer in offers) {
								if (offer.CatalogId != lastCatalogId) {
									lastCatalogId = offer.CatalogId;
									lastGroup = !lastGroup;
								}
								offer.IsGrouped = lastGroup;
							}
							return offers;
						}
					}))
				.Switch()
				.ObserveOn(UiScheduler)
				//будь бдителен CalculateRetailCost и LoadOrderItems может вызвать обращение к базе если данные еще не загружены
				//тк синхронизация не производится загрузка должна выполняться в основной нитке
				.Do(v => {
					LoadOrderItems(v);
					Calculate(v);
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

#if DEBUG
		public override object[] GetRebuildArgs()
		{
			return new object[] {
				id
			};
		}
#endif
	}
}