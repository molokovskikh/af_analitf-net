﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

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

		[Test]
		public void ChangeAddressImport_future_data()
		{
			Address address = session.Query<Address>().FirstOrDefault();
			if (address != null)

			{
				WaybillSettings ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
				if (ws != null)
				{
					using (var transaction = session.BeginTransaction())
					{
						address.Name = "тестовый адрес доставки до изменения";
						ws.Address = "тестовый адрес доставки до изменения";
						session.Save(ws);
						transaction.Commit();
					}
					Assert.AreEqual("тестовый адрес доставки до изменения", ws.Address);

					var data = new List<Tuple<string, string[]>> {
						Tuple.Create(TempFile("Addresses.txt", address.Id.ToString() + "\tизмененый тестовый адрес доставки\t0\tЮридическое лицо\t", System.Text.Encoding.GetEncoding(1251)), new[] { "truncate", "Id", "Name", "HaveLimits", "Org"})};
					using (var transaction = session.BeginTransaction())
					{
						var cmd = InitCmd(new ImportCommand(data)
						{
							Strict = false
						});
						cmd.Execute();
						session.Flush();
						transaction.Commit();
					}
					ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
					Assert.AreEqual("измененый тестовый адрес доставки", ws.Address);
					// проверка на неизменость WaybillSettings.Address
					//задаем начальные условия
					using (var transaction = session.BeginTransaction())
					{
						address.Name = "тестовый адрес доставки до изменения";
						ws.Address = "тестовый адрес доставки до изменения";
						session.Save(ws);
						transaction.Commit();
					}
					//меняем адрес
					using (var transaction = session.BeginTransaction())
					{
						ws.Address = "тестовый адрес доставки после ручного изменения";
						session.Save(ws);
						transaction.Commit();
					}
					Assert.AreEqual("тестовый адрес доставки после ручного изменения", ws.Address);
					// проводим загрузку
					var data1 = new List<Tuple<string, string[]>> { Tuple.Create(TempFile("Users.txt", "5\ttest\t"), new[] { "Id", "NonExistsColumn" }) };
					using (var transaction = session.BeginTransaction())
					{
						var cmd = InitCmd(new ImportCommand(data1)
						{
							Strict = false
						});
						cmd.Execute();
						session.Flush();
						transaction.Commit();
					}
					ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
					Assert.AreEqual("тестовый адрес доставки после ручного изменения", ws.Address);
	
					using (var transaction = session.BeginTransaction())
					{
						address.Name = "тестовый адрес доставки до изменения";
						ws.Address = "тестовый адрес доставки до изменения";
						session.Save(ws);
						transaction.Commit();
					}
					Assert.AreEqual("тестовый адрес доставки до изменения", ws.Address);

					using (var transaction = session.BeginTransaction())
					{
						ws.Address = "тестовый адрес доставки после ручного изменения";
						session.Save(ws);
						transaction.Commit();
					}
					ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
					Assert.AreEqual("тестовый адрес доставки после ручного изменения", ws.Address);

					data = new List<Tuple<string, string[]>> {
						Tuple.Create(TempFile("Addresses.txt", address.Id.ToString() + "\tтестовый адрес доставки до изменения\t0\tЮридическое лицо\t", System.Text.Encoding.GetEncoding(1251)), new[] { "truncate", "Id", "Name", "HaveLimits", "Org"})};
					using (var transaction = session.BeginTransaction())
					{
						var cmd = InitCmd(new ImportCommand(data)
						{
							Strict = false
						});
						cmd.Execute();
						session.Flush();
						transaction.Commit();
					}
					ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
					Assert.AreEqual("тестовый адрес доставки после ручного изменения", ws.Address);

					using (var transaction = session.BeginTransaction())
					{
						address.Name = "тестовый адрес доставки до изменения";
						ws.Address = "тестовый адрес доставки до изменения";
						session.Save(ws);
						transaction.Commit();
					}
					Assert.AreEqual("тестовый адрес доставки до изменения", ws.Address);

					using (var transaction = session.BeginTransaction())
					{
						ws.Address = "тестовый адрес доставки после ручного изменения";
						session.Save(ws);
						transaction.Commit();
					}
					ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
					Assert.AreEqual("тестовый адрес доставки после ручного изменения", ws.Address);

					data = new List<Tuple<string, string[]>> {
						Tuple.Create(TempFile("Addresses.txt", address.Id.ToString() + "\tтестовый адрес доставки после изменения\t0\tЮридическое лицо\t", System.Text.Encoding.GetEncoding(1251)), new[] { "truncate", "Id", "Name", "HaveLimits", "Org"})};
					using (var transaction = session.BeginTransaction())
					{
						var cmd = InitCmd(new ImportCommand(data)
						{
							Strict = false
						});
						cmd.Execute();
						session.Flush();
						transaction.Commit();
					}
					ws = session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == address.Id);
					Assert.AreEqual("тестовый адрес доставки после ручного изменения", ws.Address);
				}
			}
		}
	}
}