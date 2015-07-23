using System.IO;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class RejectsFixture : BaseViewFixture
	{
		[Test]
		public void Show_view()
		{
			var model = new RejectsViewModel();
			Bind(model);
			scheduler.Start();

			Assert.IsTrue(model.CanExport);
			var result = (OpenResult)model.Export();
			Assert.IsTrue(File.Exists(result.Filename));
		}
	}
}