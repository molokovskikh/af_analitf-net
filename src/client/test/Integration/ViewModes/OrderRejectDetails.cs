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
			model.CurrentLine.Value = model.Lines.Value[0];
			model.EnterLine();
			var offers = (SearchOfferViewModel)shell.ActiveItem;
			testScheduler.Start();
			Assert.That(offers.Offers.Value.Count, Is.GreaterThan(0));
		}
	}
}