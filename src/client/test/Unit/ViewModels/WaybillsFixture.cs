﻿using System.Collections.ObjectModel;
using System.IO;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class WaybillsFixture : BaseUnitFixture
	{
		[Test]
		public void Report()
		{
			FileHelper.InitDir(shell.Config.TmpDir);
			var model = new WaybillsViewModel();
			model.Waybills.Value = new ObservableCollection<Waybill>();
			Activate(model);
			var result = (OpenResult) model.AltExport();
			Assert.IsTrue(File.Exists(result.Filename));
		}
	}
}