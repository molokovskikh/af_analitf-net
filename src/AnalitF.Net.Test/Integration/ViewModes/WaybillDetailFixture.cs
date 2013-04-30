using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
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
			session.Save(waybill);
			session.Flush();

			model = Init(new WaybillDetails(waybill.Id));
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
			var doc = (FixedDocument)((PrintPreviewViewModel)result.Model).Document;
			foreach (var page in doc.Pages) {
				PrintFixture.SaveToPng(page.Child, "1.png", new Size(600, 800));
			}
			Process.Start("mspaint", "1.png");
		}
	}
}