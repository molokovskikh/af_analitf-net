using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class SettingsFixture : ViewModelFixture<SettingsViewModel>
	{
		[SetUp]
		public void Setup()
		{
			restore = true;
		}

		[Test]
		public void Calculate_base_category()
		{
			model.Settings.Value.GroupByProduct = !model.Settings.Value.GroupByProduct;
			var results = model.Save().ToList();
		}

		[Test]
		public void Not_save_invalid_data()
		{
			var origin = model.Markups.Value.Count;
			Assert.AreEqual(0, model.Markups.Value[0].Begin);
			model.Markups.Value.Add(new MarkupConfig());
			var results = model.Save().ToList();
			Assert.AreEqual("Некорректно введены границы цен.", manager.MessageBoxes.Implode());

			Reset();
			var all = session.Query<MarkupConfig>().ToList();
			Assert.AreEqual(origin, model.Markups.Value.Count);
			//не должно быть потерянных записей
			Assert.AreEqual(model.Markups.Value.Count + model.VitallyImportantMarkups.Value.Count, all.Count);
		}

		[Test]
		public void Save_changes()
		{
			model.Markups.Value.RemoveEach(model.Markups.Value);
			model.Markups.Value.Add(new MarkupConfig());
			var results = model.Save().ToList();
			Close(model);

			Assert.AreEqual("", manager.MessageBoxes.Implode());

			Reset();
			Assert.That(model.Markups.Value.Count, Is.EqualTo(1));
		}

		[Test]
		public void Do_not_flush_changes_by_default()
		{
			var value = Generator.Random(10000).First();
			model.Settings.Value.OverCostWarningPercent = value;
			Close(model);
			session.Refresh(settings);
			Assert.That(settings.OverCostWarningPercent, Is.Not.EqualTo(value));
		}

		[Test]
		public void Save_waybill_settings()
		{
			model.CurrentWaybillSettings.Value.Name = "test";
			var results = model.Save().ToList();
			Close(model);
			var waybillSettings = session.Load<WaybillSettings>(model.CurrentWaybillSettings.Value.Id);
			Assert.AreEqual("test", waybillSettings.Name);
		}

		[Test]
		public void Filter_not_exists_dir_maps()
		{
			var notExistsId = session.Query<Supplier>().ToArray().Max(s => s.Id) + Generator.Random().First();
			var dirMap = new DirMap();
			session.Save(dirMap);
			session.CreateSQLQuery("update DirMaps set SupplierId = :notExistsId where Id = :id")
				.SetParameter("notExistsId", notExistsId)
				.SetParameter("id", dirMap.Id)
				.ExecuteUpdate();
			Assert.IsEmpty(model.DirMaps.Where(m => m.Id == dirMap.Id).ToArray());
		}

		[Test]
		public void Edit_color()
		{
			var appStyle = model.Styles.First(s => s.Name == "Junk");
			var results = model.EditColor(appStyle).GetEnumerator();
			results.MoveNext();
			var dialog = (NativeDialogResult<ColorDialog>)results.Current;
			Assert.AreEqual(ColorHelper.ToHexString(dialog.Dialog.Color), appStyle.Background);
			dialog.Dialog.Color = Color.FromArgb(255, 10, 0);
			results.MoveNext();
			Assert.AreEqual("#FFFF0A00", appStyle.Background);
		}

		[Test]
		public void Recalculate_junk_on_junk_period_change()
		{
			var offer = session.Query<Offer>().First();
			var newOffer = new Offer(offer.Price, offer, offer.Cost);
			newOffer.Exp = DateTime.Now.AddMonths(9);
			newOffer.Id.OfferId += (ulong)Generator.Random().First();
			newOffer.OriginalJunk = false;
			newOffer.Junk = false;
			session.Save(newOffer);

			model.Settings.Value.JunkPeriod = 10;
			TaskResult(model.Save());

			session.Refresh(newOffer);
			Assert.IsTrue(newOffer.Junk, newOffer.Id.ToString());
			Assert.IsFalse(newOffer.OriginalJunk);
		}
	}
}