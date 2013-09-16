using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class StyleBuilderFixture
	{
		private ResourceDictionary resource;
		private ResourceDictionary appResource;

		[SetUp]
		public void Setup()
		{
			resource = new ResourceDictionary();
			appResource = new ResourceDictionary();
		}

		[Test]
		public void Build_style()
		{
			Build(typeof(WaybillLine));
			Assert.IsTrue(resource.Contains("WaybillLineNdsCell"));
			Assert.IsTrue(resource.Contains("WaybillLineIsNdsInvalidLegend"), "WaybillLineIsNdsInvalidLegend");
			Assert.IsTrue(resource.Contains("WaybillLineRow"), "WaybillLineRow");
		}

		[Test]
		public void Respect_known_styles()
		{
			Build(typeof(Mnn));
			Assert.IsTrue(resource.Contains("MnnRow"));
			var style = (Style)resource["MnnRow"];
			var setter = style.Triggers.OfType<MultiDataTrigger>()
				.SelectMany(t => t.Setters)
				.OfType<Setter>().First(s => s.Property == Control.BackgroundProperty);
			Assert.AreEqual(Colors.Silver, ((SolidColorBrush)setter.Value).Color);
		}

		private void Build(Type type)
		{
			StyleHelper.BuildStyles(resource, appResource, type, Colors.White, Colors.Black);
		}
	}
}