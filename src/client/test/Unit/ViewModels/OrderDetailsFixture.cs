using System;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using NUnit.Framework;
using System.Windows;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
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

		[Test]
		public void Delete_line_confirm()
		{
			var price = new Price("тестовый");
			var order = new Order(new Address("тестовый"), new Offer(price, 150) {
				RequestRatio = 15
			}, 15);
			var model = new OrderDetailsViewModel(order);
			manager.DefaultQuestsionResult = MessageBoxResult.No;
			model.CurrentLine.Value = model.Lines.Value[0];
			model.Delete();
			model.OfferUpdated();
			Assert.AreEqual(1, model.Lines.Value.Count);
			manager.DefaultQuestsionResult = MessageBoxResult.Yes;
			model.CurrentLine.Value = model.Lines.Value[0];
			model.Delete();
			model.OfferUpdated();
			Assert.AreEqual(0, model.Lines.Value.Count);
		}
	}
}