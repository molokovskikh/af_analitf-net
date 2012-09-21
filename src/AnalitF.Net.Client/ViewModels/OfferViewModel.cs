using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class OfferViewModel : Screen
	{
		public OfferViewModel(ISession session, CatalogName name, CatalogForm form)
		{
			//var catalogs = session.Query<Catalog>().Where(c => c.Name == name && c.Form == form).ToList();
			//var ids = catalogs.Select(c => c.Id).ToArray();
			//Offers = session.Query<Offer>().Where(o => ids.Contains(o.ProductId)).ToList();
			Offers = new List<Offer> {
				new Offer(),
				new Offer(),
				new Offer()
			};
		}

		public List<Offer> Offers { get; set; }

		public void Exit()
		{
			TryClose();
		}
	}
}