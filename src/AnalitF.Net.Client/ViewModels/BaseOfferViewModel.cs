using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using Message = Common.Tools.Message;

namespace AnalitF.Net.Client.ViewModels
{
	public abstract class BaseOfferViewModel : BaseScreen, IExportable
	{
		private readonly TimeSpan warningTimeout = TimeSpan.FromSeconds(5);

		private Catalog currentCatalog;
		protected List<string> producers;
		protected List<Offer> offers;
		protected string currentProducer;
		private Offer currentOffer;
		protected List<MarkupConfig> markups = new List<MarkupConfig>();
		private string orderWarning;
		//тк уведомление о сохранении изменний приходит после
		//изменения текущего предложения
		private Offer lastEditOffer;

		protected bool NeedToCalculateDiff;

		private ExcelExporter excelExporter;

		public BaseOfferViewModel()
		{
			markups = Session.Query<MarkupConfig>().ToList();
			excelExporter = new ExcelExporter(this);

			this.ObservableForProperty(m => m.OrderWarning)
				.Where(m => !String.IsNullOrEmpty(m.Value))
				.Throttle(warningTimeout)
				.Subscribe(m => { OrderWarning = null; });

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(m => NotifyOfPropertyChange("CurrentOrder"));
		}

		protected void UpdateProducers()
		{
			var offerProducers = Offers.Select(o => o.Producer).Distinct().OrderBy(p => p);
			Producers = new[] { Consts.AllProducerLabel }.Concat(offerProducers).ToList();
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				NotifyOfPropertyChange("CurrentCatalog");
				NotifyOfPropertyChange("CanShowDescription");
			}
		}

		public List<string> Producers
		{
			get { return producers; }
			set
			{
				producers = value;
				NotifyOfPropertyChange("Producers");
			}
		}

		[Export]
		public List<Offer> Offers
		{
			get { return offers; }
			set
			{
				offers = value;
				NotifyOfPropertyChange("Offers");
			}
		}

		public string CurrentProducer
		{
			get { return currentProducer; }
			set
			{
				currentProducer = value;
				NotifyOfPropertyChange("CurrentProducer");
			}
		}

		public Offer CurrentOffer
		{
			get { return currentOffer; }
			set
			{
				currentOffer = value;
				NotifyOfPropertyChange("CurrentOffer");
				if (currentOffer != null && (currentCatalog == null || CurrentCatalog.Id != currentOffer.CatalogId))
					CurrentCatalog = Session.Load<Catalog>(currentOffer.CatalogId);
			}
		}

		public Order CurrentOrder
		{
			get
			{
				if (CurrentOffer == null)
					return null;
				if (CurrentOffer.OrderLine == null)
					return null;
				return CurrentOffer.OrderLine.Order;
			}
		}

		public bool CanShowCatalogWithMnnFilter
		{
			get { return CurrentCatalog != null && CurrentCatalog.Name.Mnn != null; }
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

		public void ShowCatalogWithMnnFilter()
		{
			if (!CanShowCatalogWithMnnFilter)
				return;

			Shell.ActivateItem(new CatalogViewModel {
				FiltredMnn = CurrentCatalog.Name.Mnn
			});
		}

		public void ShowCatalog()
		{
			if (CurrentOffer == null)
				return;

			var catalogViewModel = new CatalogViewModel();
			catalogViewModel.CurrentCatalogName = catalogViewModel.CatalogNames.FirstOrDefault(c => c.Id == CurrentCatalog.Name.Id);
			catalogViewModel.CurrentCatalog = catalogViewModel.CatalogForms.FirstOrDefault(c => c.Id == CurrentCatalog.Id);
			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog);
			offerViewModel.CurrentOffer = CurrentOffer;

			Shell.NavigateAndReset(catalogViewModel, offerViewModel);
		}

		public static List<Offer> SortByMinCostInGroup<T>(List<Offer> offer, Func<Offer, T> key)
		{
			var lookup = offer.GroupBy(key)
				.ToDictionary(g => g.Key, g => g.Min(o => o.Cost));

			var offers = offer.OrderBy(o => Tuple.Create(lookup[key(o)], o.Cost)).ToList();

			var indexes = lookup.OrderBy(k => k.Value)
				.Select((k, i) => Tuple.Create(k.Key, i))
				.ToDictionary(t => t.Item1, t => t.Item2);

			offers.Each(o => {
				o.SortKeyGroup = indexes[key(o)] % 2;
			});

			return offers;
		}

		private void CalculateRetailCost()
		{
			foreach (var offer in Offers)
				offer.CalculateRetailCost(markups);
		}

		public void OfferUpdated()
		{
			if (CurrentOffer == null)
				return;

			lastEditOffer = CurrentOffer;
			ShowValidationError(CurrentOffer.UpdateOrderLine(Address));
		}

		public void OfferCommitted()
		{
			if (lastEditOffer == null)
				return;

			ShowValidationError(lastEditOffer.SaveOrderLine(Address));
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

			//если человек ушел с этой позиции а мы откатываем значение то нужно вернуть его к этой позиции что бы он
			//мог ввести коректное значение
			if (errors.Any()) {
				if (CurrentOffer == null || CurrentOffer.Id != lastEditOffer.Id) {
					CurrentOffer = lastEditOffer;
				}
			}
		}

		protected void Calculate()
		{
			if (NeedToCalculateDiff)
				CalculateDiff();

			CalculateRetailCost();
		}

		private void CalculateDiff()
		{
			decimal baseCost = 0;
			if (Settings.DiffCalcMode == DiffCalcMode.MinCost)
				baseCost = Offers.Select(o => o.Cost).MinOrDefault();
			else if (Settings.DiffCalcMode == DiffCalcMode.MinBaseCost)
				baseCost = Offers.Where(o => o.Price.BasePrice).Select(o => o.Cost).MinOrDefault();

			foreach (var offer in Offers) {
				offer.CalculateDiff(baseCost);
				if (Settings.DiffCalcMode == DiffCalcMode.PrevOffer)
					baseCost = offer.Cost;
			}
		}

		public bool CanExport
		{
			get { return excelExporter.CanExport; }
		}

		public IResult Export()
		{
			return excelExporter.Export();
		}

		protected void LoadOrderItems()
		{
			if (Address == null)
				return;

			var lines = Session.Query<OrderLine>().Where(l => l.Order.Address == Address).ToList();

			foreach (var offer in Offers) {
				var line = lines.FirstOrDefault(l => l.OfferId == offer.Id);
				if (line != null) {
					offer.AttachOrderLine(line);
				}
			}
		}

		protected abstract void Query();

		public void Update()
		{
			Query();
			Calculate();
			LoadOrderItems();
		}

		protected override void OnDeactivate(bool close)
		{
			Session.Flush();
			base.OnDeactivate(close);
		}
	}
}
