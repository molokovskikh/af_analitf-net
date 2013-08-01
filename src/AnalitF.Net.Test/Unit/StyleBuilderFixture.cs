using System.Windows;
using System.Windows.Media;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class StyleBuilderFixture
	{
		[Test]
		public void Build_style()
		{
			var resource = new ResourceDictionary();
			var appResource = new ResourceDictionary();

			App.BuildStyles(resource, appResource, typeof(WaybillLine), Colors.White, Colors.Black);
			Assert.IsTrue(resource.Contains("WaybillLineNdsCell"));
			Assert.IsTrue(resource.Contains("WaybillLineIsNdsInvalidLegend"));
		}
	}
}