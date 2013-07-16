using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class WaybillDetailFixture : BaseFixture
	{
		private WaybillDetails model;

		[SetUp]
		public void Setup()
		{
			var waybill = new Waybill {
				Address = address
			};
			waybill.Lines = Enumerable.Range(0, 10).Select(i => new WaybillLine(waybill)).ToList();
			waybill.Lines[0].Nds = 10;
			waybill.Lines[0].ProducerCost = 15.13m;
			waybill.Lines[0].SupplierCostWithoutNds = 18.25m;
			waybill.Lines[0].SupplierCost = 19.8m;
			session.Save(waybill);
			session.Flush();

			model = Init(new WaybillDetails(waybill.Id));
		}

		[Test]
		public void Tax_filter()
		{
			Assert.AreEqual("Все, Нет значения, 10", model.Taxes.Implode(t => t.Name));
			Assert.AreEqual("Все", model.CurrentTax.Value.Name);
			model.CurrentTax.Value = model.Taxes.First(t => t.Value == 10);
			Assert.AreEqual(1, model.Lines.Value.Count);
		}

		[Test]
		public void Recalculate_waybill()
		{
			Assert.AreEqual(23.8, model.Lines.Value[0].RetailCost);
			Assert.IsTrue(model.RoundToSingleDigit);
			model.RoundToSingleDigit.Value = false;
			Assert.AreEqual(23.82, model.Lines.Value[0].RetailCost);
		}

		[Test]
		public void Open_waybill()
		{
			Assert.IsNotNull(model.Waybill);
			Assert.IsNotNull(model.Lines);
		}

		[Test, RequiresSTA]
		public void Print_racking_map()
		{
			var result = (DialogResult)model.PrintRackingMap();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview);
		}

		[Test, RequiresSTA]
		public void Print_invoice()
		{
			var result = (DialogResult)model.PrintInvoice();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview.Document);
		}
	}
}