using System;
using System.Configuration;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto.Events;
using Newtonsoft.Json;
using NUnit.Framework;
using ReactiveUI.Testing;
using CreateWaybill = AnalitF.Net.Client.ViewModels.Dialogs.CreateWaybill;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class WaybillsFixture : ViewModelFixture<WaybillsViewModel>
	{
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			waybill = Fixture<LocalWaybill>().Waybill;
		}

		[Test]
		public void Load_waybills()
		{
			Assert.IsNotNull(model.Waybills.Value);
		}

		[Test]
		public void Alt_export()
		{
			var result = (OpenResult)model.AltExport();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Delete()
		{
			var waybill = model.Waybills.Value.First(w => w.Supplier != null);
			var file = Path.Combine(settings.MapPath("Waybills"), string.Format("{0}_{1}.txt",
				waybill.Id,
				waybill.Supplier.Name));
			File.WriteAllText(file, "test content");
			model.CurrentWaybill.Value = waybill;
			model.SelectedWaybills.Add(waybill);
			model.Delete();
			Assert.That(model.Waybills.Value.Select(w => w.Id), Is.Not.Contains(waybill.Id));
			Assert.IsFalse(File.Exists(file));
		}

		[Test]
		public void Refresh_data_on_reactivate()
		{
			restore = true;

			model.CurrentWaybill.Value = model.Waybills.Value.First(w => w.Id == waybill.Id);
			model.EnterWaybill();
			Deactivate(model);

			var details = (WaybillDetails)shell.ActiveItem;
			var retailSum = details.Waybill.RetailSum;

			var settings = Init<SettingsViewModel>();
			settings.Markups.Value[0].Markup = 50;
			settings.Markups.Value[0].MaxMarkup = 50;
			var result = settings.Save().ToList();
			Close(settings);
			scheduler.AdvanceByMs(50);

			Assert.That(details.Waybill.RetailSum, Is.GreaterThan(retailSum));
			Close(details);
			Activate(model);
			var reloaded = model.Waybills.Value.First(w => w.Id == waybill.Id);
			Assert.That(reloaded.RetailSum, Is.GreaterThan(retailSum));
		}

		[Test]
		public void Create()
		{
			var result = model.Create().GetEnumerator();
			Assert.IsTrue(result.MoveNext());
			var dialog = ((CreateWaybill)((DialogResult)result.Current).Model);
			dialog.Waybill.ProviderDocumentId = "1";
			dialog.Waybill.UserSupplierName = "test";
			result.MoveNext();
			Assert.IsNotNull(dialog.Waybill.Address);
			Assert.AreEqual(dialog.Waybill.Address.Id, address.Id);
			Assert.Contains(dialog.Waybill.Id, model.Waybills.Value.Select(w => w.Id).ToArray());
		}

		[Test]
		public void Waybill_report()
		{
			FileHelper.InitDir(settings.MapPath("Reports"));
			var result = model.RegulatorReport().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);
			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Is.StringContaining("Росздравнадзор"));
		}

		[Test]
		public void Vitally_important_report()
		{
			Fixture<LocalWaybill>();
			FileHelper.InitDir(settings.MapPath("Reports"));
			var result = model.VitallyImportantReport().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);
			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Is.StringContaining("Росздравнадзор"));
			//дожна быть строка заголовка и как миниму одна строка данных
			Assert.That(File.ReadAllText(open.Filename).Length, Is.GreaterThan(1));
		}

		[Test]
		public void diadoc()
		{
			var api = new DiadocApi(ConfigurationManager.AppSettings["DiadokApi"], "https://diadoc-api.kontur.ru", new WinApiCrypt());
			var token = api.Authenticate(ConfigurationManager.AppSettings["DiadokLogin"], ConfigurationManager.AppSettings["DiadokPassword"]);
			var boxes = api.GetMyOrganizations(token).Organizations.SelectMany(x => x.Boxes);
			foreach (var box in boxes) {
				var ev = api.GetNewEvents(token, box.BoxId).Events;
				foreach (var e in ev) {
					Console.WriteLine("{3} {2} - {0} {1}", e.MessageId, e.Message != null ? e.Message.FromTitle : "<null>", e.Timestamp, e.EventId);
					foreach (var entity in e.Entities) {
						Console.WriteLine(entity);
					}
				}
			}
		}

		[Test]
		public void diadoc1()
		{
			var api = new DiadocApi(ConfigurationManager.AppSettings["DiadokApi"], "https://diadoc-api.kontur.ru", new WinApiCrypt());
			var token = api.Authenticate(ConfigurationManager.AppSettings["DiadokLogin"], ConfigurationManager.AppSettings["DiadokPassword"]);
			//var user = api.GetMyUser(token);
			Console.WriteLine(api.GetMessage(token, "99f634490f4e469da56dd47b724eba81@diadoc.ru", "02a73097-27e6-4f5f-841e-f6b3ef7d1c65", "1be8e8f2-b513-4911-ab61-f0cc83b02391"));
			//var patch = new MessagePatchToPost
			//{
			//	BoxId = "99f634490f4e469da56dd47b724eba81@diadoc.ru",
			//	MessageId = "02a73097-27e6-4f5f-841e-f6b3ef7d1c65",
			//};
			//patch.AddSignature(new DocumentSignature {
			//	ParentEntityId = "1be8e8f2-b513-4911-ab61-f0cc83b02391",
			//	//Signature = new byte[0],
			//	SignWithTestSignature = true
			//});
			//api.PostMessagePatch(token, patch);
			var boxes = api.GetMyOrganizations(token).Organizations.SelectMany(x => x.Boxes);
			foreach (var box in boxes) {
				Console.WriteLine("box id = {0}", box.BoxId);
				var docs = api.GetDocuments(token, new DocumentsFilter {
					BoxId = box.BoxId,
					FilterCategory = "Any.Inbound"
				});
				foreach (var doc in docs.Documents) {
					Console.WriteLine(JsonConvert.SerializeObject(doc, Formatting.Indented));
					var m = api.GetMessage(token, box.BoxId, doc.MessageId);
					//Console.WriteLine(m.MessageId);
					//foreach (var entity in m.Entities) {
					//	Console.WriteLine("{0} {1} {2} {3}", entity.EntityId, entity.EntityType, entity.AttachmentType, entity.FileName);
					//}
					Console.WriteLine(JsonConvert.SerializeObject(m, Formatting.Indented));
				}
			}
		}
	}
}