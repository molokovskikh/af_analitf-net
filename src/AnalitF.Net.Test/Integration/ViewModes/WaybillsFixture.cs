using System.IO;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class WaybillsFixture : BaseFixture
	{
		private WaybillsViewModel model;

		[SetUp]
		public void Setup()
		{
			model = Init<WaybillsViewModel>();
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
	}
}