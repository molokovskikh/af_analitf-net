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
		private CatalogForm currentCatalogForm;
		private Catalog currentCatalog;

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
				RaisePropertyChangedEventImmediately("CurrentCatalogName");
				RaisePropertyChangedEventImmediately("CanShowDescription");
				LoadCatalogForms();
			}
		}

		public CatalogForm CurrentCatalogForm
		{
			get { return currentCatalogForm; }
			set
			{
				currentCatalogForm = value;
				RaisePropertyChangedEventImmediately("CurrentCatalogForm");
				CurrentCatalog = session.Query<Catalog>().First(c => c.Name == CurrentCatalogName && c.Form == CurrentCatalogForm);
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

		public bool CanShowDescription
		{
			get { return CurrentCatalogName != null && CurrentCatalogName.Description != null; }
		}

		public void ShowDescription()
		{
			Shell.ActivateItem(new DescriptionViewModel(CurrentCatalogName.Description));
		}
	}
}