using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SearchOfferViewModel : BaseOfferViewModel
	{
		private string searchText;

		public SearchOfferViewModel()
		{
			DisplayName = "Поиск в прайс-листах";
		}

		public void Search()
		{
			if (string.IsNullOrEmpty(SearchText))
				return;

			Offers = Session.Query<Offer>().Where(o => o.ProductSynonym.Contains(SearchText)).ToList();
		}

		public string SearchText
		{
			get { return searchText; }
			set
			{
				searchText = value;
				RaisePropertyChangedEventImmediately("SearchText");
			}
		}
	}
}