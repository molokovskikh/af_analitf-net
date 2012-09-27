using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogViewModel : BaseScreen
	{
		private CatalogName _currentCatalogName;
		private Catalog currentCatalogForm;
		private Catalog currentCatalog;

		private List<CatalogName> _catalogNames;
		private List<Catalog> _catalogForms;
		private bool showWithoutOffers;

		public CatalogViewModel()
		{
			DisplayName = "Поиск препаратов в каталоге";
			Update();
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

		public List<Catalog> CatalogForms
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
				RaisePropertyChangedEventImmediately("CurrentCatalogName");
				RaisePropertyChangedEventImmediately("CanShowDescription");
				LoadCatalogForms();
			}
		}

		public Catalog CurrentCatalogForm
		{
			get { return currentCatalogForm; }
			set
			{
				currentCatalogForm = value;
				CurrentCatalog = value;
				RaisePropertyChangedEventImmediately("CurrentCatalogForm");
			}
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				RaisePropertyChangedEventImmediately("CurrentCatalog");
			}
		}

		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				RaisePropertyChangedEventImmediately("ShowWithoutOffers");
				Update();
			}
		}

		private void Update()
		{
			var catalogNames = Session.Query<CatalogName>();
			if (!ShowWithoutOffers) {
				catalogNames = catalogNames.Where(c => c.HaveOffers);
			}
			CatalogNames = catalogNames.OrderBy(c => c.Name).ToList();
		}

		private void LoadCatalogForms()
		{
			if (CurrentCatalogName == null)
				CatalogForms = Enumerable.Empty<Catalog>().ToList();

			var queryable = Session.Query<Catalog>().Where(c => c.Name == CurrentCatalogName);
			if (!ShowWithoutOffers) {
				queryable = queryable.Where(c => c.HaveOffers);
			}
			CatalogForms = queryable
				.OrderBy(c => c.Form)
				.ToList();
		}

		public void EnterCatalogForms()
		{
			Shell.ActiveAndSaveCurrent(new OfferViewModel(CurrentCatalog));
		}

		public bool CanShowDescription
		{
			get { return CurrentCatalogName != null && CurrentCatalogName.Description != null; }
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalogName.Description));
		}
	}
}