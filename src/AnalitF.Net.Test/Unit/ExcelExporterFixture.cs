using System;
using System.Collections.Generic;
using System.IO;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
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
			var exporter = new ExcelExporter(new NonExportableScreen(), Path.GetTempPath());
			Assert.IsFalse(exporter.CanExport);

			exporter = new ExcelExporter(new ExportableScreen(), Path.GetTempPath());
			Assert.IsTrue(exporter.CanExport);
		}
	}
}