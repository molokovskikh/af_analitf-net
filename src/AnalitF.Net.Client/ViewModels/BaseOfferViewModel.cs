using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class BaseOfferViewModel : BaseScreen
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

		protected const string AllProducerLabel = "Все производители";

		public BaseOfferViewModel()
		{
			markups = Session.Query<MarkupConfig>().ToList();

			this.ObservableForProperty(m => m.OrderWarning)
				.Where(m => !String.IsNullOrEmpty(m.Value))
				.Throttle(warningTimeout)
				.Subscribe(m => { OrderWarning = null; });
		}

		protected void UpdateProducers()
		{
			var offerProducers = Offers.Select(o => o.ProducerSynonym).Distinct().OrderBy(p => p);
			Producers = new[] { AllProducerLabel }.Concat(offerProducers).ToList();
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				RaisePropertyChangedEventImmediately("CurrentCatalog");
				RaisePropertyChangedEventImmediately("CanShowDescription");
			}
		}

		public List<string> Producers
		{
			get { return producers; }
			set
			{
				producers = value;
				RaisePropertyChangedEventImmediately("Producers");
			}
		}

		public List<Offer> Offers
		{
			get { return offers; }
			set
			{
				offers = value;
				RaisePropertyChangedEventImmediately("Offers");
			}
		}

		public string CurrentProducer
		{
			get { return currentProducer; }
			set
			{
				currentProducer = value;
				RaisePropertyChangedEventImmediately("CurrentProducer");
			}
		}

		public Offer CurrentOffer
		{
			get { return currentOffer; }
			set
			{
				currentOffer = value;
				RaisePropertyChangedEventImmediately("CurrentOffer");
				if (currentOffer != null && (currentCatalog == null || CurrentCatalog.Id != currentOffer.CatalogId))
					CurrentCatalog = Session.Load<Catalog>(currentOffer.CatalogId);
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
				RaisePropertyChangedEventImmediately("OrderWarning");
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

			var catalogViewModel = new CatalogViewModel {
				CurrentCatalog = CurrentCatalog
			};
			var offerViewModel = new OfferViewModel(CurrentCatalog);
			offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == CurrentOffer.Id);

			Shell.Navigate(catalogViewModel, offerViewModel);
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

		protected void CalculateRetailCost()
		{
			foreach (var offer in Offers)
				offer.CalculateRetailCost(markups);
		}

		public void OfferUpdated()
		{
			if (CurrentOffer == null)
				return;

			lastEditOffer = CurrentOffer;
			CurrentOffer.MakePreorderCheck();
			ProcessMessages(CurrentOffer);
		}

		public void OfferCommitted()
		{
			if (lastEditOffer == null)
				return;

			var order = lastEditOffer.UpdateOrderLine();
			ProcessMessages(lastEditOffer);
			if (order.IsEmpty) {
				Session.Delete(order);
			}
			else {
				Session.SaveOrUpdate(order);
			}
			Session.Flush();
		}

		private void ProcessMessages(Offer offer)
		{
			OrderWarning = CurrentOffer.Warning;
			if (!String.IsNullOrEmpty(offer.Notification)) {
				Manager.ShowMessageBox(offer.Notification, "АналитФАРМАЦИЯ: Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
				offer.Notification = null;
				//если человек ушел с этой позиции а мы откатываем значение то нужно вернуть его к этой позиции что бы он
				//мог ввести коректное значение
				if (CurrentOffer == null || CurrentOffer.Id != offer.Id) {
					CurrentOffer = offer;
				}
			}
		}
	}
}
