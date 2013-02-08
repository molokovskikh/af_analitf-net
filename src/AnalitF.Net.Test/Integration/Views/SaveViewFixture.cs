using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class SaveViewFixture : BaseViewFixture
	{
		private CatalogOfferViewModel model;
		private CatalogOfferView view;
		private Catalog catalog;

		[SetUp]
		public void Setup()
		{
			catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			InitView();
		}

		private void InitView()
		{
			model = Init(new CatalogOfferViewModel(catalog));
			view = InitView<CatalogOfferView>(model);
		}

		[Test]
		public void Save_settings()
		{
			ScreenExtensions.TryDeactivate(model, true);

			var settings = shell.ViewSettings["CatalogOfferViewModel.Offers"];
			Assert.That(settings.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Serialize_data()
		{
			ScreenExtensions.TryDeactivate(model, true);

			var data = JsonConvert.SerializeObject(shell);
			shell.ViewSettings.Clear();

			JsonConvert.PopulateObject(data, shell);
			var settings = shell.ViewSettings["CatalogOfferViewModel.Offers"];
			Assert.That(settings.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Restore_settings()
		{
			var grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Offers");
			grid.Columns[0].Visibility = Visibility.Collapsed;

			ScreenExtensions.TryDeactivate(model, true);
			InitView();
			grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Offers");
			Assert.That(grid.Columns[0].Visibility, Is.EqualTo(Visibility.Collapsed));
		}

		[Test]
		public void Reset_settings()
		{
			var grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Offers");
			grid.Columns[0].Visibility = Visibility.Collapsed;
			model.ResetView(grid);
			Assert.That(grid.Columns[0].Visibility, Is.EqualTo(Visibility.Visible));
		}
	}
}