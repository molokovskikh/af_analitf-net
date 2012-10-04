using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class BaseOfferViewModel : BaseScreen
	{
		private Catalog currentCatalog;
		protected List<string> producers;
		protected List<Offer> offers;
		protected string currentProducer;
		private Offer currentOffer;

		protected const string AllProducerLabel = "Все производители";

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

		public bool CanShowDescription
		{
			get
			{
				return CurrentCatalog != null
					&& CurrentCatalog.Name.Description != null;
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

			Shell.CancelNavigation();
			TryClose();
			Shell.PushInChain(new CatalogViewModel {
				CurrentCatalog = CurrentCatalog
			});
			var offerViewModel = new OfferViewModel(CurrentCatalog);
			offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == CurrentOffer.Id);
			Shell.ActivateItem(offerViewModel);
		}
	}
}
