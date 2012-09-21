using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogViewModel : BaseScreen
	{
		private CatalogName _currentCatalogName;

		private List<CatalogName> _catalogNames;
		private List<CatalogForm> _catalogForms;

		public CatalogViewModel()
		{
			CatalogNames = session.Query<CatalogName>().OrderBy(c => c.Name).ToList();
		}

		public List<CatalogName> CatalogNames
		{
			get { return _catalogNames; }
			set
			{
				_catalogNames = value;
				NotifyOfPropertyChange("CatalogNames");
			}
		}

		public List<CatalogForm> CatalogForms
		{
			get { return _catalogForms; }
			set
			{
				_catalogForms = value;
				NotifyOfPropertyChange("CatalogForms");
			}
		}

		public CatalogName CurrentCatalogName
		{
			get { return _currentCatalogName; }
			set
			{
				_currentCatalogName = value;
				LoadCatalogForms();
			}
		}

		public CatalogForm CurrentCatalogForm { get; set; }

		private void LoadCatalogForms()
		{
			if (_currentCatalogName == null)
				CatalogForms = Enumerable.Empty<CatalogForm>().ToList();

			CatalogForms = session.Query<Catalog>()
				.Where(c => c.Name == _currentCatalogName)
				.Select(c => c.Form)
				.ToList();
		}

		public void EnterCatalogForms()
		{
			Shell.ActiveAndSaveCurrent(new OfferViewModel(session, CurrentCatalogName, CurrentCatalogForm));
		}
	}
}