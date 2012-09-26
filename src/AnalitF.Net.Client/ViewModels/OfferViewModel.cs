using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class OfferViewModel : BaseOfferViewModel
	{
		public string currentRegion;
		public List<string> regions;

		private const string allRegionLabel = "Все регионы";

		public OfferViewModel(Catalog catalog)
		{
			DisplayName = "Сводный прайс-лист";
			CurrentCatalog = catalog;

			this.ObservableForProperty(m => m.CurrentRegion)
				.Subscribe(e => Filter());
		}

		private void Filter()
		{
			Offers = Session.Query<Offer>().Where(o => o.CatalogId == CurrentCatalog.Id).ToList();
			Regions = Offers.Select(o => o.RegionName).ToList();
		}

		public List<Offer> Offers { get; set; }

		public string[] Filters { get; set; }

		public List<string> Regions
		{
			get { return regions; }
			set
			{
				regions = value;
				RaisePropertyChangedEventImmediately("Regions");
			}
		}

		public string CurrentRegion
		{
			get { return currentRegion; }
			set
			{
				currentRegion = value;
				RaisePropertyChangedEventImmediately("CurrentRegion");
			}
		}
	}
}