using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.Views.Offers;
using Caliburn.Micro;
using Common.Tools;
using Newtonsoft.Json;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
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
			model = new CatalogOfferViewModel(catalog);
			view = (CatalogOfferView)Bind(model);
			model.SaveDefaults(view);
		}

		[Test]
		public void Save_settings()
		{
			Close(model);

			var settings = shell.ViewSettings["CatalogOfferViewModel.Offers"];
			Assert.That(settings.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Serialize_data()
		{
			Close(model);

			var data = JsonConvert.SerializeObject(shell);
			shell.ViewSettings.Clear();

			JsonConvert.PopulateObject(data, shell);
			var settings = shell.ViewSettings["CatalogOfferViewModel.Offers"];
			Assert.That(settings.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Restore_settings()
		{
			var grid = view.Descendants<DataGrid2>().First(c => c.Name == "Offers");
			grid.Columns[0].Visibility = Visibility.Collapsed;

			Close(model);
			InitView();
			grid = view.Descendants<DataGrid2>().First(c => c.Name == "Offers");
			Assert.That(grid.Columns[0].Visibility, Is.EqualTo(Visibility.Collapsed));
		}

		[Test]
		public void Restore_display_index()
		{
			var grid = view.Descendants<DataGrid2>().First(c => c.Name == "Offers");
			ForceBinding(view);
			grid.Columns[0].DisplayIndex = 5;
			model.ResetView(grid);
			Assert.That(grid.Columns[0].DisplayIndex, Is.EqualTo(0),
				grid.Columns.Select(c => Tuple.Create(c.DisplayIndex, c.Header)).Implode());
		}

		[Test]
		public void Reset_settings()
		{
			var grid = view.Descendants<DataGrid2>().First(c => c.Name == "Offers");
			grid.Columns[0].Visibility = Visibility.Collapsed;
			model.ResetView(grid);
			Assert.That(grid.Columns[0].Visibility, Is.EqualTo(Visibility.Visible));
		}

		[Test]
		public void Do_not_override_user_settings_activation()
		{
			Close(model);
			InitView();

			var grid = view.Descendants<DataGrid2>().First(c => c.Name == "Offers");
			var column = grid.Columns[0];
			Assert.AreNotEqual(351, column.Width.Value);
			column.Width = new DataGridLength(351);

			view.SetValue(ViewModelBinder.ConventionsAppliedProperty, true);
			//AttachView вызывается каждый раз при активации\деактивации
			//в этом случае не нужно сбрасывать настройки
			((IViewAware)model).AttachView(view);
			Assert.AreEqual(351, column.Width.Value);
		}

		[Test]
		public void Restore_column_width_view_close()
		{
			var grid = view.Descendants<DataGrid2>().First(c => c.Name == "HistoryOrders");
			grid.Columns[0].Width = new DataGridLength(15);
			grid.Columns[1].Width = new DataGridLength(15);
			grid.Columns[2].Width = new DataGridLength(15);
			grid.Columns[3].Width = new DataGridLength(150);
			grid.Columns[4].Width = new DataGridLength(15);
			grid.Columns[5].Width = new DataGridLength(125);
			var saveWidth = grid.Columns[5].ActualWidth;
			Close(model);
			InitView();
			grid = view.Descendants<DataGrid2>().First(c => c.Name == "HistoryOrders");
			Assert.AreEqual(saveWidth, grid.Columns[5].Width.Value);
		}

		[Test]
		public void Restore_column_width_view_deactivate()
		{
			var grid = view.Descendants<DataGrid2>().First(c => c.Name == "HistoryOrders");
			grid.Columns[0].Width = new DataGridLength(15);
			grid.Columns[1].Width = new DataGridLength(15);
			grid.Columns[2].Width = new DataGridLength(15);
			grid.Columns[3].Width = new DataGridLength(150);
			grid.Columns[4].Width = new DataGridLength(15);
			grid.Columns[5].Width = new DataGridLength(125);
			var saveWidth = grid.Columns[5].ActualWidth;
			Deactivate(model);
			Activate(model);
			grid = view.Descendants<DataGrid2>().First(c => c.Name == "HistoryOrders");
			Assert.AreEqual(saveWidth, grid.Columns[5].Width.Value);
		}
	}
}