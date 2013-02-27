using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Windows;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderLinesViewModel : BaseOrderViewModel
	{
		private OrderLine currentLine;
		private Catalog currentCatalog;
		private List<MarkupConfig> markups;
		private Price currentPrice;
		private List<OrderLine> lines;
		private List<SentOrderLine> sentLines;
		private string orderWarning;

		public OrderLinesViewModel()
		{
			AllOrders = new NotifyValue<bool>();
			AddressesEnabled = new NotifyValue<bool>(() => AllOrders.Value, AllOrders);

			DisplayName = "Сводный заказ";
			Addresses = Session.Query<Address>()
				.OrderBy(a => a.Name)
				.Select(a => new Selectable<Address>(a)).ToList();

			markups = Session.Query<MarkupConfig>().ToList();

			Dep(m => m.CanShowCatalog,
				m => m.CurrentCatalog);

			Dep(m => m.CanShowDescription,
				m => m.CurrentCatalog);

			Dep(m => m.CanShowCatalogWithMnnFilter,
				m => m.CurrentCatalog);

			Dep(m => m.CurrentCatalog,
				m => m.CurrentLine);

			Dep(m => m.CurrentOffer,
				m => m.CurrentLine);

			Dep(m => m.Sum, m => m.Lines);
			Dep(m => m.CanDelete, m => m.CurrentLine, m => m.IsCurrentSelected);

			Addresses.Select(a => Observable.FromEventPattern<PropertyChangedEventArgs>(a, "PropertyChanged"))
				.Merge()
				.Throttle(TimeSpan.FromMilliseconds(500), Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => Update());

			Dep(Update, m => m.CurrentPrice, m => m.AllOrders.Value);

			//пока устанавливаем значения не надо оповещать об изменения
			//все равно будет запрос когда форма активируется
			IsNotifying = false;
			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = Prices.FirstOrDefault();

			Begin = DateTime.Today;
			End = DateTime.Today;
			IsNotifying = true;
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
		}

		public override void Update()
		{
			if (IsCurrentSelected) {
				var query = Session.Query<OrderLine>();

				if (CurrentPrice != null && CurrentPrice.Id != null) {
					query = query.Where(l => l.Order.Price == CurrentPrice);
				}

				if (!AllOrders.Value) {
					query = query.Where(l => l.Order.Address == Address);
				}
				else {
					var addresses = Addresses.Where(i => i.IsSelected).Select(i => i.Item).ToArray();
					query = query.Where(l => addresses.Contains(l.Order.Address));
				}

				Lines = query
					.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProducerSynonym)
					.ToList();

				CalculateRetailCost();
			}
			else {
				var query = StatelessSession.Query<SentOrderLine>()
					.Fetch(l => l.Order)
					.ThenFetch(o => o.Price)
					.Where(l => l.Order.SentOn > Begin && l.Order.SentOn < End.AddDays(1));

				if (CurrentPrice != null && CurrentPrice.Id != null) {
					query = query.Where(l => l.Order.Price == CurrentPrice);
				}

				SentLines = query.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProductSynonym)
					.ToList();
			}
		}
		private void Dep(Action action, params Expression<Func<OrderLinesViewModel, object>>[] to)
		{
			to.Select(e => this.ObservableForProperty(e))
				.Merge()
				.Subscribe(e => action());
		}

		protected void Dep(Expression<Func<OrderLinesViewModel, object>> from, params Expression<Func<OrderLinesViewModel, object>>[] to)
		{
			var name = @from.GetProperty();
			to.Select(e => this.ObservableForProperty(e))
				.Merge()
				.Subscribe(e => NotifyOfPropertyChange(name));
		}

		protected void CalculateRetailCost()
		{
			foreach (var offer in Lines)
				offer.CalculateRetailCost(markups);
		}

		public NotifyValue<bool> AllOrders { get; set; }

		public bool AllOrdersVisible
		{
			get { return Addresses.Count > 1; }
		}

		public IList<Selectable<Address>> Addresses { get; set; }

		public bool AddressesVisible
		{
			get { return Addresses.Count > 1; }
		}

		public NotifyValue<bool> AddressesEnabled { get; set; }

		public List<Price> Prices { get; set; }

		public Price CurrentPrice
		{
			get { return currentPrice; }
			set
			{
				currentPrice = value;
				NotifyOfPropertyChange("CurrentPrice");
			}
		}

		[Export]
		public List<OrderLine> Lines
		{
			get { return lines; }
			set
			{
				lines = value;
				NotifyOfPropertyChange("Lines");
			}
		}

		[Export]
		public List<SentOrderLine> SentLines
		{
			get { return sentLines; }
			set
			{
				sentLines = value;
				NotifyOfPropertyChange("SentLines");
			}
		}

		public decimal Sum
		{
			get
			{
				return Lines.Sum(l => l.Sum);
			}
		}

		public Catalog CurrentCatalog
		{
			get
			{
				if (CurrentLine == null)
					return null;
				if (currentCatalog == null || currentCatalog.Id != CurrentLine.CatalogId)
					currentCatalog = Session.Load<Catalog>(CurrentLine.CatalogId);
				return currentCatalog;
			}
		}

		public BaseOffer CurrentOffer
		{
			get { return CurrentLine; }
		}

		public OrderLine CurrentLine
		{
			get { return currentLine; }
			set
			{
				currentLine = value;
				NotifyOfPropertyChange("CurrentLine");
			}
		}

		public bool CanShowDescription
		{
			get
			{
				return CurrentCatalog != null
					&& CurrentCatalog.Name.Description != null;
			}
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalog.Name.Description));
		}

		public bool CanDelete
		{
			get { return CurrentLine != null && IsCurrentSelected; }
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить позицию?"))
				return;

			var offer = Session.Load<Offer>(CurrentLine.OfferId);
			offer.AttachOrderLine(CurrentLine);
			offer.OrderCount = 0;
			var error = offer.UpdateOrderLine(Address, Settings);
			ShowValidationError(error);

			Update();
		}

		private void ShowValidationError(List<Message> messages)
		{
			var warnings = messages.Where(m => m.IsWarning).Implode(Environment.NewLine);
			//нельзя перетирать старые предупреждения, предупреждения очищаются только по таймеру
			if (!String.IsNullOrEmpty(warnings))
				OrderWarning = warnings;

			var errors = messages.Where(m => m.IsError);
			foreach (var message in errors) {
				Manager.Warning(message.MessageText);
			}
		}

		public string OrderWarning
		{
			get { return orderWarning; }
			set
			{
				orderWarning = value;
				NotifyOfPropertyChange("OrderWarning");
			}
		}

		public bool CanShowCatalog
		{
			get { return CurrentCatalog != null && CurrentOffer != null; }
		}

		public void EnterLine()
		{
			ShowCatalog();
		}

		public void ShowCatalog()
		{
			if (!CanShowCatalog)
				return;

			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog);
			offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == CurrentLine.OfferId);

			Shell.Navigate(offerViewModel);
		}

		public bool CanShowCatalogWithMnnFilter
		{
			get { return CurrentCatalog != null && CurrentCatalog.Name.Mnn != null; }
		}

		public void ShowCatalogWithMnnFilter()
		{
			if (!CanShowCatalogWithMnnFilter)
				return;

			var catalogViewModel = new CatalogViewModel {
				FiltredMnn = CurrentCatalog.Name.Mnn
			};
			Shell.Navigate(catalogViewModel);
		}
	}
}