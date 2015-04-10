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
			ScreenExtensions.TryDeactivate(model, true);
			model = new OrderLinesViewModel();
			Activate(model);
			Assert.IsTrue(model.IsSentSelected.Value);
			Assert.IsTrue(model.EndEnabled.Value);
			Assert.IsTrue(model.BeginEnabled.Value);
		}

		[Test]
		public void Persis_address_settings()
		{
			model.AddressSelector.Addresses.Add(new Selectable<Address>(new Address("тест1") { Id = 1 }));
			model.AddressSelector.Addresses.Add(new Selectable<Address>(new Address("тест2") { Id = 2 }));
			Activate(model);
			model.AddressSelector.All.Value = true;
			model.AddressSelector.Addresses[0].IsSelected = false;
			ScreenExtensions.TryDeactivate(model, true);

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
			model.AddressSelector.Addresses.Add(new Selectable<Address>(new Address("тест1") { Id = 1 }));
			model.AddressSelector.Addresses.Add(new Selectable<Address>(new Address("тест2") { Id = 2 }));
			Activate(model);
			Assert.IsTrue(model.AddressSelector.All.Value);
			Assert.IsFalse(model.AddressSelector.Addresses[0].IsSelected);
			Assert.IsTrue(model.AddressSelector.Addresses[1].IsSelected);
		}
	}
}