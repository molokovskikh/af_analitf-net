using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class WaybillDetailsFixture : BaseUnitFixture
	{
		[Test]
		public void Set_rounding()
		{
			var model = new WaybillDetails(1);
			model.Waybill = new Waybill(model.Address, new Supplier());
			Activate(model);

			Assert.IsTrue(model.IsActive);
			Assert.AreEqual(Rounding.To0_10, model.Waybill.Rounding);
			model.Waybill.Rounding = Rounding.None;
		}
	}
}