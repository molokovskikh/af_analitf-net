using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.NHibernate;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	public class OrderLines : BaseUnitFixture
	{
		private OrderLinesViewModel model;

		[SetUp]
		public void SetUp()
		{
			model = new OrderLinesViewModel();
		}

		[Test]
		public void Update_calendar_on_session_load()
		{
			Activate(model);
			model.IsSentSelected.Value = true;
			model.IsCurrentSelected.Value = false;
			Close(model);
			model = new OrderLinesViewModel();
			Activate(model);
			Assert.IsTrue(model.IsSentSelected.Value);
			Assert.IsTrue(model.EndEnabled.Value);
			Assert.IsTrue(model.BeginEnabled.Value);
		}

		[Test]
		public void Persis_address_settings()
		{
			Activate(model, new Address("тест1") { Id = 1 }, new Address("тест2") { Id = 2 });
			model.AddressSelector.All.Value = true;
			model.AddressSelector.Addresses[0].IsSelected = false;
			Close(model);

			var memory = new MemoryStream();
			var writer = new StreamWriter(memory);
			var serializer = new JsonSerializer {
				ContractResolver = new NHibernateResolver()
			};
			serializer.Serialize(writer, shell);
			writer.Flush();

			memory.Position = 0;
			shell.PersistentContext.Clear();
			serializer.Populate(new StreamReader(memory), shell);

			model = new OrderLinesViewModel();
			Activate(model, new Address("тест1") { Id = 1 }, new Address("тест2") { Id = 2 });
			Assert.IsTrue(model.AddressSelector.All.Value);
			Assert.IsFalse(model.AddressSelector.Addresses[0].IsSelected);
			Assert.IsTrue(model.AddressSelector.Addresses[1].IsSelected);
		}

		[Test]
		public void Rollback_invalid_edit_values()
		{
			var price = new Price("тестовый");
			var order = new Order(new Address("тестовый"), new Offer(price, 150) {
				RequestRatio = 15
			}, 15);
			order.TryOrder(new Offer(price, 180) { RequestRatio = 7}, 7);
			Activate(model);
			model.Lines.Value = new ObservableCollection<OrderLine>(order.Lines);
			model.CurrentLine.Value = model.Lines.Value[0];
			model.CurrentLine.Value.Count = 9;
			model.Editor.Updated();
			//симулируем переход на другую строку
			model.CurrentLine.Value = model.Lines.Value[1];
			model.Editor.Committed();

			Assert.AreEqual(15, model.Lines.Value[0].Count);
		}

		[Test]
		public void Reset_edit_on_close()
		{
			var price = new Price("тестовый");
			var order = new Order(new Address("тестовый"), new Offer(price, 150) {
				RequestRatio = 15
			}, 15);
			order.TryOrder(new Offer(price, 180) { RequestRatio = 7}, 7);
			Activate(model);
			model.Lines.Value = new ObservableCollection<OrderLine>(order.Lines);
			model.CurrentLine.Value = model.Lines.Value[0];
			model.CurrentLine.Value.Count = 9;
			model.Editor.Updated();
			Close(model);
			model.TryClose();

			Assert.AreEqual(15, model.Lines.Value[0].Count);
		}

		private void Close(Screen model)
		{
			ScreenExtensions.TryDeactivate(model, true);
		}
	}
}