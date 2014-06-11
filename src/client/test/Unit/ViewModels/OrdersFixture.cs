using System;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class OrdersFixture : BaseUnitFixture
	{
		[Test]
		public void Save_address_filter()
		{
			var model = new OrdersViewModel();
			var addresses = new [] {
				new Address("тест") {
					Id = 1
				},
				new Address("Тестовый адрес доставки 1") {
					Id = 2
				},
			};
			addresses.Each(a => model.AddressSelector.Addresses.Add(new Selectable<Address>(a)));
			Activate(model, addresses[0]);
			model.AddressSelector.All.Value = true;
			model.AddressSelector.Addresses[1].IsSelected = false;
			ScreenExtensions.TryDeactivate(model, true);

			model = new OrdersViewModel();
			addresses.Each(a => model.AddressSelector.Addresses.Add(new Selectable<Address>(a)));
			Activate(model, addresses[0]);
			Assert.IsTrue(model.AddressSelector.All.Value);
			Assert.IsFalse(model.AddressSelector.Addresses[1].IsSelected);
		}
	}
}