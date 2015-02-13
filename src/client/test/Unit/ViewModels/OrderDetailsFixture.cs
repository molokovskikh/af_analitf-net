using System;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class OrderDetailsFixture : BaseUnitFixture
	{
		[Test]
		public void Export()
		{
			user.Permissions.Clear();
			var order = new Order(new Address("тест"), new Offer(new Price("тест"), 100));
			var model = new OrderDetailsViewModel(order);
			Assert.IsFalse(model.CanExport);

			user.Permissions.Add(new Permission("ECOO"));
			model = new OrderDetailsViewModel(order);
			Assert.IsTrue(model.CanExport.Value);
		}
	}
}