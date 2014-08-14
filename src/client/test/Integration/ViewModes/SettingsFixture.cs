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
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
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
			model.Save();
		}

		[Test]
		public void Not_save_invalid_data()
		{
			var origin = model.Markups.Count;
			Assert.AreEqual(0, model.Markups[0].Begin);
			model.Markups.Add(new MarkupConfig());
			model.Save();
			Assert.AreEqual("Некорректно введены границы цен.", manager.MessageBoxes.Implode());

			Reset();
			var all = session.Query<MarkupConfig>().ToList();
			Assert.AreEqual(origin, model.Markups.Count);
			//не должно быть потерянных записей
			Assert.AreEqual(model.Markups.Count + model.VitallyImportantMarkups.Count, all.Count);
		}

		[Test]
		public void Save_changes()
		{
			model.Markups.RemoveEach(model.Markups);
			model.Markups.Add(new MarkupConfig());
			model.Save();
			Close(model);

			Assert.AreEqual("", manager.MessageBoxes.Implode());

			Reset();
			Assert.That(model.Markups.Count, Is.EqualTo(1));
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
			model.Save();
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
	}
}