using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using NHibernate.Linq;
using NHibernate.Util;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderRejectDetails : ViewModelFixture
	{
		[Test]
		public void Show_details()
		{
			var id = session.Query<Waybill>().First(w => w.DocType == DocType.Reject).Id;
			var model = new Client.ViewModels.OrderRejectDetails(id);
			Init(model);
			Assert.IsNotNull(model.Doc.Value);
			Assert.That(model.Lines.Value.Count, Is.GreaterThan(0));
			//для первой могу не найтись предложения
			model.CurrentLine.Value = model.Lines.Value.Skip(1).First(l => l.ProductId == null && l.Count == 1);
			testScheduler.Start();
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));

			model.CurrentLine.Value = model.Lines.Value.First(l => l.ProductId != null);
			testScheduler.Start();
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));
		}
	}
}