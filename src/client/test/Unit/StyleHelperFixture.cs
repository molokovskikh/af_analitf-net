using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture, Apartment(ApartmentState.STA)]
	public class StyleHelperFixture
	{
		private ResourceDictionary resource;
		private ResourceDictionary appResource;

		[SetUp]
		public void Setup()
		{
			StyleHelper.Reset();
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
			var setter = Background(style);
			Assert.AreEqual(Colors.Silver, ((SolidColorBrush)setter.Value).Color);
		}

		[Test]
		public void Include_in_legend_only_posible_styles()
		{
			StyleHelper.BuildStyles(appResource);
			var grid = new DataGrid();
			grid.Columns.Add(new DataGridTextColumn {
				Binding = new Binding("Period")
			});
			Build(typeof(Offer));
			Assert.AreEqual("Подсказка\r\nЖизненно важные препараты\r\nПрепарат запрещен к заказу\r\nУцененные препараты", Legend(grid, typeof(Offer)));
		}

		[Test]
		public void Check_context_on_build_legend()
		{
			StyleHelper.BuildStyles(appResource);
			var grid = new DataGrid();
			grid.Columns.Add(new DataGridTextColumn {
				Binding = new Binding("Sum")
			});
			Build(typeof(Order));

			Assert.AreEqual("Подсказка\r\n\"Заморожен\"", Legend(grid, typeof(Order)));
			Assert.AreEqual("Подсказка\r\n\"Заморожен\"" +
				"\r\nИмеется позиция с корректировкой по цене и/или по количеству", Legend(grid, typeof(Order), "CorrectionEnabled"));
		}

		[Test]
		public void Legend_for_waybillLine()
		{
			var styles = StyleHelper.GetDefaultStyles();
			var style = styles.First(s => s.Name == "IsRetailCostFixed");
			Assert.IsTrue(style.IsBackground);
			Assert.AreEqual("#FFFA9BFA", style.Background);
			Assert.AreEqual("Black", style.Foreground);
			Assert.AreEqual("Розничная цена: редактирование запрещено поставщиком", style.Description);

			StyleHelper.BuildStyles(appResource);
			var grid = new DataGrid();
			grid.Columns.Add(new DataGridTextColumn
			{
				Binding = new Binding("RetailCost")
			});
			Build(typeof(WaybillLine));
			var legend = Legend(grid, typeof(WaybillLine));
			Assert.AreEqual("Подсказка\r\nЗабракованная позиция\r\nНовая забракованная позиция\r\nНовая разбракованная позиция\r\nРозничная цена: не рассчитана\r\nРозничная цена: редактирование запрещено поставщиком", legend);
		}


		[Test]
		public void Get_default_styles()
		{
			var styles = StyleHelper.GetDefaultStyles();
			Assert.That(styles.Count, Is.GreaterThan(0));
			var style = styles.First(s => s.Name == "IsSendError");
			Assert.IsTrue(style.IsBackground);
			Assert.AreEqual("#FFA0A0A4", style.Background);
			Assert.AreEqual("Black", style.Foreground);
			Assert.AreEqual("Корректировка по цене и/или по количеству", style.Description);
		}

		[Test]
		public void Build_user_style_for_legend()
		{
			StyleHelper.UserStyles.Add("Leader", new Setter(Control.BackgroundProperty, Brushes.Aqua));
			Build(typeof(Offer));
			var style = (Style)resource["OfferLeaderLegend"];
			var background = style.Setters.OfType<Setter>().First(s => s.Property == Control.BackgroundProperty);
			Assert.AreEqual(Brushes.Aqua.Color, ((SolidColorBrush)background.Value).Color);
		}

		[Test]
		public void User_style_foreground()
		{
			var userStyles = new[] {
				new CustomStyle {
					Foreground = "Red",
					Name = "VitallyImportant"
				}
			};
			StyleHelper.BuildStyles(appResource, userStyles);
			var style = (Style)appResource["OfferRow"];
			var foreground = Foreground(style);
			Assert.AreEqual(Colors.Red, ((SolidColorBrush)foreground.Value).Color);
			appResource.Values.OfType<Style>().Each(s => s.Seal());
		}

		[Test]
		public void Use_style_name()
		{
			Build(typeof(WaybillLine));
			var style = (Style)resource["WaybillLineRow"];
			var trigger = style.Triggers.OfType<DataTrigger>().First(t => t.Setters.OfType<Setter>().Any(s => s.Property == Control.ForegroundProperty));
			Assert.AreEqual(Colors.Green, ((SolidColorBrush)Foreground(style).Value).Color);
			Assert.AreEqual("ActualVitallyImportant", ((Binding)trigger.Binding).Path.Path);
		}

		[Test]
		public void Get_default_style()
		{
			var styles = StyleHelper.GetDefaultStyles();
			var style = styles.First(s => s.Name == "NotBase");
			Assert.AreEqual("#FFF0F0F0", style.Background);
		}

		[Test]
		public void Apply_style_to_template_column()
		{
			Build(typeof(WaybillLine));
			var grid = new DataGrid2();
			var column = new DataGridTemplateColumn();
			column.SetValue(FrameworkElement.NameProperty, "CertificateLink");
			grid.Columns.Add(column);
			StyleHelper.ApplyStyles(typeof(WaybillLine), grid, resource);
			Assert.IsNotNull(column.CellStyle);
		}

		private static Setter Background(Style style)
		{
			return style.Triggers.OfType<MultiDataTrigger>()
				.SelectMany(t => t.Setters)
				.OfType<Setter>()
				.First(s => s.Property == Control.BackgroundProperty);
		}

		private static Setter Foreground(Style style)
		{
			return style.Triggers.OfType<MultiDataTrigger>().SelectMany(t => t.Setters)
				.Concat(style.Triggers.OfType<DataTrigger>().SelectMany(t => t.Setters))
				.OfType<Setter>()
				.First(s => s.Property == Control.ForegroundProperty);
		}

		private string Legend(DataGrid grid, Type type, string context = null)
		{
			var legend = new StackPanel();
			StyleHelper.ApplyStyles(type, grid, appResource, legend, context);
			return legend.AsText();
		}

		private void Build(Type type)
		{
			StyleHelper.BuildStyles(resource, appResource, type, Colors.White, Colors.Black);
		}
	}
}