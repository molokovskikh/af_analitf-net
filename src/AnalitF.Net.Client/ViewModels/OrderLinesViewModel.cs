using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Windows;
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

		private Address address;

		public OrderLinesViewModel()
		{
			DisplayName = "Сводный заказ";
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
			Dep(m => m.CanDelete, m => m.CurrentLine);

			Dep(Update, m => m.CurrentPrice, m => m.Begin, m => m.End);

			Update();

			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = Prices.First();

			Begin = DateTime.Today;
			End = DateTime.Today;
		}

		public override void Update()
		{
			if (IsCurrentSelected) {
				var query = Session.Query<OrderLine>();

				if (CurrentPrice != null && CurrentPrice.Id != 0) {
					query = query.Where(l => l.Order.Price == CurrentPrice);
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

				if (CurrentPrice != null && CurrentPrice.Id != 0) {
					query = query.Where(l => l.Order.Price == CurrentPrice);
				}

				SentLines = query.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProductSynonym)
					.ToList();
			}
		}

		private void Dep(Action action, params Expression<Func<OrderLinesViewModel, object>>[] to)
		{
			to.Select(e => this.ObservableForProperty(e)).Merge()
				.Subscribe(e => action());
		}

		protected void Dep(Expression<Func<OrderLinesViewModel, object>> from, Expression<Func<OrderLinesViewModel, object>> to)
		{
			var name = @from.GetProperty();
			this.ObservableForProperty(to)
				.Subscribe(e => NotifyOfPropertyChange(name));
		}

		protected void CalculateRetailCost()
		{
			foreach (var offer in Lines)
				offer.CalculateRetailCost(markups);
		}

		public virtual List<Price> Prices { get; set; }

		public Price CurrentPrice
		{
			get { return currentPrice; }
			set
			{
				currentPrice = value;
				NotifyOfPropertyChange("CurrentPrice");
			}
		}

		public virtual List<OrderLine> Lines
		{
			get { return lines; }
			set
			{
				lines = value;
				NotifyOfPropertyChange("Lines");
			}
		}

		public virtual List<SentOrderLine> SentLines
		{
			get { return sentLines; }
			set
			{
				sentLines = value;
				NotifyOfPropertyChange("SentLines");
			}
		}

		public virtual decimal Sum
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
			get { return CurrentLine != null; }
		}

		public void Delete()
		{
			if (CurrentLine == null)
				return;

			var result = Manager.ShowMessageBox("Удалить позицию?", "АналитФАРМАЦИЯ: Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
				return;

			var offer = Session.Load<Offer>(CurrentLine.OfferId);
			offer.OrderCount = 0;
			var order = offer.UpdateOrderLine(address);

			if (order != null) {
				if (order.IsEmpty) {
					Session.Delete(order);
				}
				else {
					Session.SaveOrUpdate(order);
				}
				Session.Flush();
			}
			Update();
		}

		public bool CanShowCatalog
		{
			get { return CurrentCatalog != null && CurrentOffer != null; }
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