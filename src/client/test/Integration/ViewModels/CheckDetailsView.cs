using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using System.IO;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using System.Collections.Generic;
using System;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class CheckDetailsView : ViewModelFixture<CheckDetails>
	{
		[Test]
		public void Export_check_details()
		{
			var result = (OpenResult)model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_checksDetails()
		{
			var results = model.PrintCheckDetails().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}
	}
}
