using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NPOI.SS.UserModel;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class ExcelExporterFixture
	{
		public class NonExportableScreen : Screen
		{
		}

		public class ExportableScreen : Screen
		{
			[Export]
			public List<Tuple<string>> Items { get; set; }
		}

		[Test]
		public void Calculate_can_export()
		{
			var exporter = new ExcelExporter(new NonExportableScreen(), new string[0], Path.GetTempPath());
			Assert.IsFalse(exporter.CanExport);

			exporter = new ExcelExporter(new ExportableScreen(), new[] { "Items" }, Path.GetTempPath());
			Assert.IsTrue(exporter.CanExport);
		}

		[Test]
		public void Export_date_time()
		{
			var book = ExcelExporter.ExportTable(new[] { "DateTime" },
				new[] { new object[] { new DateTime(2015, 6, 22, 14, 49, 00) } });
			var value = new DataFormatter().FormatCellValue(book.GetSheet("Экспорт").GetRow(1).GetCell(0));
			Assert.AreEqual("22.06.2015 14:49:00", value);
		}
	}
}