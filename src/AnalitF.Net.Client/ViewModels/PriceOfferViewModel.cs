using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class HistoryOrdersViewModel : Screen
	{
		public HistoryOrdersViewModel(Catalog catalog, Offer offer, List<SentOrderLine> lines)
		{
			Offer = offer;
			Catalog = catalog;
			Lines = lines;
			DisplayName = "Предыдущие заказы";
		}

		public Offer Offer { get; set; }

		public Catalog Catalog { get; set; }

		public List<SentOrderLine> Lines { get; set; }
	}

	public class PriceOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private string[] filters = new[] {
			"Прайс-лист (F4)",
			"Заказы (F5)",
			"Лучшие предложения (F6)",
		};

		private string currentFilter;
		private string activeSearchTerm;

		public PriceOfferViewModel(Price price, bool showLeaders)
		{
			SearchText = new NotifyValue<string>();
			Price = new NotifyValue<Price>(price);
			DisplayName = "Заявка поставщику";

			Filters = filters;
			currentProducer = Consts.AllProducerLabel;
			currentFilter = filters[0];
			if (showLeaders)
				currentFilter = filters[2];

			this.ObservableForProperty(m => m.CurrentFilter)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Subscribe(e => Update());

			this.ObservableForProperty(m => m.Price.Value.Order)
				.Subscribe(_ => NotifyOfPropertyChange("CanDeleteOrder"));
		}

		public NotifyValue<Price> Price { get; set; }

		public string[] Filters { get; set; }

		public string ActiveSearchTerm
		{
			get { return activeSearchTerm; }
			set
			{
				activeSearchTerm = value;
				NotifyOfPropertyChange("ActiveSearchTerm");
			}
		}

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				NotifyOfPropertyChange("CurrentFilter");
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
			UpdateProducers();
		}

		protected override void Query()
		{
			var query = StatelessSession.Query<Offer>().Where(o => o.Price.Id == Price.Value.Id);
			if (CurrentProducer != Consts.AllProducerLabel) {
				query = query.Where(o => o.Producer == CurrentProducer);
			}
			if (CurrentFilter == filters[2]) {
				query = query.Where(o => o.LeaderPrice == Price.Value);
			}
			if (currentFilter == filters[1]) {
				//если мы установили фильтр по заказанным позициям то нужно
				//выполнить сохранение
				Session.Flush();
				query = query.Where(o => StatelessSession.Query<OrderLine>().Count(l => l.OfferId == o.Id
					&& l.Order.Address == Address
					&& l.Order.Price == Price.Value) > 0);
			}
			if (!String.IsNullOrEmpty(ActiveSearchTerm)) {
				query = query.Where(o => o.ProductSynonym.Contains(ActiveSearchTerm));
			}

			Offers = query
				.Fetch(o => o.Price)
				.Fetch(o => o.LeaderPrice)
				.ToList();
			CurrentOffer = offers.FirstOrDefault();
			if (CurrentOffer != null)
				Price.Value = CurrentOffer.Price;
		}

		public NotifyValue<string> SearchText { get; set; }

		public void CancelFilter()
		{
			CurrentFilter = Filters[0];
		}

		public void FilterOrdered()
		{
			CurrentFilter = Filters[1];
		}

		public void FilterLeader()
		{
			CurrentFilter = Filters[2];
		}

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText.Value) || SearchText.Value.Length < 3) {
				return HandledResult.Skip();
			}

			ActiveSearchTerm = SearchText;
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public IResult ClearSearch()
		{
			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm = "";
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			var doc = new PriceOfferDocument(offers, Price, Address).BuildDocument();
			return new PrintResult(doc, DisplayName);
		}

		public bool CanShowHistoryOrders
		{
			get { return CurrentCatalog != null; }
		}

		public IResult ShowHistoryOrders()
		{
			if (!CanShowHistoryOrders)
				return null;

			LoadHistoryOrders();
			return new DialogResult(new HistoryOrdersViewModel(CurrentCatalog, CurrentOffer, HistoryOrders));
		}

		public IResult EnterOffer()
		{
			return ShowHistoryOrders();
		}

		public bool CanDeleteOrder
		{
			get
			{
				return Price.Value.Order != null && Address != null;
			}
		}

		public void DeleteOrder()
		{
			if (!CanDeleteOrder)
				return;

			if (!Confirm("Удалить весь заказ по данному прайс-листу?"))
				return;

			Address.Orders.Remove(Price.Value.Order);
			Price.Value.Order = null;
			foreach (var offer in Offers) {
				offer.OrderLine = null;
			}
		}
	}
}