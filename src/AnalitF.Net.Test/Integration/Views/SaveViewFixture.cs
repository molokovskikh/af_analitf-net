using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
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
		public void Restore_display_index()
		{
			var grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Offers");
			ForceBinding(view);
			grid.Columns[0].DisplayIndex = 5;
			model.ResetView(grid);
			Assert.That(grid.Columns[0].DisplayIndex, Is.EqualTo(0),
				grid.Columns.Select(c => Tuple.Create(c.DisplayIndex, c.Header)).Implode());
		}

		[Test]
		public void Reset_settings()
		{
			var grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Offers");
			grid.Columns[0].Visibility = Visibility.Collapsed;
			model.ResetView(grid);
			Assert.That(grid.Columns[0].Visibility, Is.EqualTo(Visibility.Visible));
		}

		[Test]
		public void Do_not_override_user_settings_activation()
		{
			ScreenExtensions.TryDeactivate(model, true);
			InitView();

			var grid = view.DeepChildren().OfType<DataGrid>().First(c => c.Name == "Offers");
			var column = grid.Columns[0];
			Assert.AreNotEqual(351, column.Width.Value);
			column.Width = new DataGridLength(351);

			view.SetValue(ViewModelBinder.ConventionsAppliedProperty, true);
			//AttachView вызывается каждый раз при активации\деактивации
			//в этом случае не нужно сбрасывать настройки
			((IViewAware)model).AttachView(view);
			Assert.AreEqual(351, column.Width.Value);
		}
	}
}