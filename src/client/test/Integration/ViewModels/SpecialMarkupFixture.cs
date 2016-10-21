using System;
using System.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class SpecialMarkupFixture : ViewModelFixture
	{
		[SetUp]
		public void Setup()
		{
			restore = true;
		}

		[Test]
		public void Settings()
		{
			var model = new SettingsViewModel();
			Open(model);
			model.CurrentProduct.Value = model.Products.Value.First();
			model.SpecialMarkupCheck();

			Assert.AreEqual(1, model.SpecialMarkupProducts.Value.Count);
			model.CurrentSpecialMarkupProduct.Value = model.SpecialMarkupProducts.Value.First();
			model.SpecialMarkupUncheck();
			Assert.AreEqual(0, model.SpecialMarkupProducts.Value.Count);

			model.CurrentProduct.Value = model.Products.Value[1];
			model.SpecialMarkupCheck();

			var markups = model.SpecialMarkups.Value;
			Assert.AreEqual("Special: 0 - 1000000 20%", markups.Implode());
			markups[0].End = 500;
			var markup = new MarkupConfig();
			model.NewSpecialMarkup(new InitializingNewItemEventArgs(markup));
			markup.Begin = 500;
			markup.End = 10000;
			markup.Markup = 20;
			markup.MaxMarkup = 50;
			model.SpecialMarkups.Value.Add(markup);

			model.Save().ToList();
			Close(model);
			Assert.AreEqual(1, session.Query<SpecialMarkupCatalog>().Count());
			session.Refresh(settings);
			Assert.AreEqual("Special: 0 - 500 20%, Special: 500 - 10000 20%",
				settings.Markups.Where(x => x.Type == MarkupType.Special).Implode());
		}

		[Test]
		public void SpecialMarkup_Waybill()
		{
			var waybill = new Waybill(address, session.Query<Supplier>().First());
			var catalog = session.Query<Catalog>().First();
			var product = session.Query<Product>().First(x => x.CatalogId == catalog.Id);
			var line = new WaybillLine(waybill) {
				ProductId = product.Id,
				CatalogId = catalog.Id,
				Quantity = 10,
				Nds = 10,
				ProducerCost = 15.13m,
				SupplierCostWithoutNds = 18.25m,
				SupplierCost = 20.8m
			};
			waybill.AddLine(line);
			session.Save(waybill);

			var markup = settings.Markups.First(x => x.Type == MarkupType.Special);
			markup.Markup = 50;
			session.Save(new SpecialMarkupCatalog {
				CatalogId = waybill.Lines[0].CatalogId.Value
			});

			var model = Open(new WaybillDetails(waybill.Id));
			var waybillLine = model.Waybill.Lines[0];
			Assert.AreEqual(30.8, waybillLine.RetailCost);
		}
	}
}