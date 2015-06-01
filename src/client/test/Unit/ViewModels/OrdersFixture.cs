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
			var addresses = new[] {
				new Address("тест") {
					Id = 1
				},
				new Address("Тестовый адрес доставки 1") {
					Id = 2
				},
			};
			Activate(model, addresses);
			model.AddressSelector.All.Value = true;
			model.AddressSelector.Addresses[1].IsSelected = false;
			ScreenExtensions.TryDeactivate(model, true);

			model = new OrdersViewModel();
			Activate(model, addresses);
			Assert.IsTrue(model.AddressSelector.All.Value);
			Assert.IsFalse(model.AddressSelector.Addresses[1].IsSelected);
		}

		[Test]
		public void Rebuild_address()
		{
			var model = new OrdersViewModel();
			var addresses = new[] {
				new Address("Тестовый адрес доставки 1") {
					Id = 1
				},
				new Address("Тестовый адрес доставки 2") {
					Id = 2
				},
			};
			Activate(model, addresses);
			model.AddressSelector.All.Value = true;
			model.AddressSelector.Addresses[1].IsSelected = false;
			ScreenExtensions.TryDeactivate(model, true);

			model = new OrdersViewModel();
			addresses = new[] {
				new Address("Тестовый адрес доставки 1") {
					Id = 2
				},
				new Address("Тестовый адрес доставки 3") {
					Id = 3
				},
			};
			Activate(model, addresses);
			Assert.IsTrue(model.AddressSelector.All.Value);
			Assert.IsFalse(model.AddressSelector.Addresses[0].IsSelected);
			Assert.IsTrue(model.AddressSelector.Addresses[1].IsSelected);
		}
	}
}