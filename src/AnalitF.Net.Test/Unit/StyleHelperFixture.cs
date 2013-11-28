using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NPOI.HSSF.Record.Chart;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class StyleHelperFixture
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

		[Test]
		public void Include_in_legend_only_posible_styles()
		{
			StyleHelper.Reset();
			StyleHelper.CollectStyles(appResource);
			var grid = new DataGrid();
			grid.Columns.Add(new DataGridTextColumn {
				Binding = new Binding("Period")
			});
			var legend = new StackPanel();
			Build(typeof(Offer));
			StyleHelper.ApplyStyles(typeof(Offer), grid, appResource, legend);
			Assert.AreEqual("Подсказка\r\nЖизненно важные препараты\r\nУцененные препараты", legend.AsText());
		}

		private void Build(Type type)
		{
			StyleHelper.BuildStyles(resource, appResource, type, Colors.White, Colors.Black);
		}
	}
}