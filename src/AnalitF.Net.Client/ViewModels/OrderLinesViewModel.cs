using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderLinesViewModel : BaseScreen
	{
		private OrderLine currentLine;
		private Catalog currentCatalog;

		public OrderLinesViewModel()
		{
			DisplayName = "Сводный заказ";

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

			Lines = Session.Query<OrderLine>()
				.OrderBy(l => l.ProductSynonym)
				.ThenBy(l => l.ProducerSynonym)
				.ToList();
		}

		protected void Dep(Expression<Func<OrderLinesViewModel, object>> from, Expression<Func<OrderLinesViewModel, object>> to)
		{
			var name = @from.GetProperty();
			this.ObservableForProperty(to)
				.Subscribe(e => RaisePropertyChangedEventImmediately(name));
		}

		public virtual List<Price> Prices { get; set; }
		public virtual List<OrderLine> Lines { get; set; }

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
				RaisePropertyChangedEventImmediately("CurrentLine");
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

		public void Delete()
		{

		}

		public bool CanShowCatalog
		{
			get { return CurrentCatalog != null && CurrentOffer != null; }
		}

		public void ShowCatalog()
		{
			if (!CanShowCatalog)
				return;

			var offerViewModel = new OfferViewModel(CurrentCatalog);
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