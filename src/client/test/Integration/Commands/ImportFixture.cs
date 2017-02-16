using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using AnalitF.Net.Client.Test.TestHelpers;
using Test.Support;
using NHibernate;
//using Common.Tools.Helpers;
using System.IO;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class ImportFixture : DbFixture
	{
		[Test]
		public void Import_future_data()
		{
			restore = true;
			var data = new List<Tuple<string, string[]>> {
				Tuple.Create(TempFile("Users.txt", "5\ttest\t"), new[] { "Id", "NonExistsColumn" })
			};
			var cmd = InitCmd(new ImportCommand(data)
			{
				Strict = false
			});
			cmd.Execute();
		}
	}

	public class ImportCommandFixture : MixedFixture
	{
		[Test]
		public void ChangeAddressImport_future_data()
		{
			var fixtureAddressChange = Fixture<CreateAddress>();
			var fixtureAddressNotChange = Fixture<CreateAddress>();

			User user = new User();
			Address AddressChange = new Address("тестовый адрес доставки до изменения");
			WaybillSettings WaybillSettingsChange = new WaybillSettings(user, AddressChange);

			Address AddressNotChange = new Address("тестовый адрес доставки до изменения");
			WaybillSettings WaybillSettingsNotChange = new WaybillSettings(user, AddressNotChange);


			using (var transaction = localSession.BeginTransaction())
			{
				AddressChange.Id = fixtureAddressChange.Address.Id;
				AddressNotChange.Id = fixtureAddressNotChange.Address.Id;
				WaybillSettingsChange.Address = "тестовый адрес доставки до изменения";
				WaybillSettingsNotChange.Address = "тестовый адрес доставки после ручного изменения";

				localSession.Save(AddressChange);
				localSession.Save(AddressNotChange);
				localSession.Save(WaybillSettingsChange);
				localSession.Save(WaybillSettingsNotChange);
				transaction.Commit();
			}

			localSession.Clear();
			Run(new UpdateCommand());

			WaybillSettingsChange = localSession.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == AddressChange.Id);
			Assert.AreEqual(fixtureAddressChange.Address.Value, WaybillSettingsChange.Address);
			WaybillSettingsNotChange = localSession.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == AddressNotChange.Id);
			Assert.AreEqual("тестовый адрес доставки после ручного изменения", WaybillSettingsNotChange.Address);
		}

	}
}