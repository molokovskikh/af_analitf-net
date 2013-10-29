using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NHibernate.Hql.Ast.ANTLR;

namespace AnalitF.Net.Client
{
	public partial class App : Application
	{
		private Color inactiveColor;
		private Color activeColor;
		private ResourceDictionary resources;
		private Style baseStyle;

		public SplashScreen Splash;
		public bool FaultInject;

		public void RegisterResources()
		{
			activeColor = Color.FromRgb(0xD7, 0xF0, 0xFF);
			inactiveColor = Color.FromRgb(0xDA, 0xDA, 0xDA);

			resources = Resources.MergedDictionaries[1];

			baseStyle = (Style)Resources[typeof(DataGridCell)];

			var style = new Style(typeof(DataGridCell), baseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty,
				new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("CountColumn", style);

			resources.Add("VitallyImportant", BaseStyle(activeColor, inactiveColor));

			style = CellStyle(activeColor, inactiveColor, "Junk", true, Color.FromRgb(0xf2, 0x9e, 0x66));
			resources.Add("Junk", style);

			style = CellStyle(activeColor, inactiveColor, "Leader", true, Color.FromRgb(0xC0, 0xDC, 0xC0));
			resources.Add("Leader", style);

			style = CellStyle(activeColor,
				inactiveColor,
				"Price.BasePrice",
				false,
				Color.FromRgb(0xF0, 0xF0, 0xF0));
			resources.Add("NotBaseOffer", style);

			style = new Style(typeof(DataGridCell), baseStyle);
			StyleHelper.AddTriggers(style,
				"BeginOverlap",
				true,
				Color.FromRgb(0x80, 0x80, 0),
				activeColor,
				inactiveColor);
			StyleHelper.AddTriggers(style,
				"HaveGap",
				true,
				Color.FromRgb(0x80, 0, 0),
				activeColor,
				inactiveColor);
			resources.Add("BeginMarkup", style);

			SimpleStyle("EndMarkup", "EndLessThanBegin", Colors.Red);

			var offerBaseStyle = (Style)Resources["VitallyImportant"];
			style = new Style(typeof(DataGridCell), offerBaseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty,
				new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("OrderColumn", style);

			var ignore = new [] { typeof(BaseOffer) };
			var types = GetType().Assembly.GetTypes()
				.Except(ignore)
				.Where(t => t.GetProperties().Any(p => p.GetCustomAttributes(typeof(StyleAttribute), true).Length > 0))
				.ToArray();
			foreach (var type in types) {
				var mainStyle = type == typeof (WaybillLine)
					? (Style)Resources["DefaultEditableCell"]
					: baseStyle;
				StyleHelper.BuildStyles(resources,
					Resources,
					type,
					activeColor,
					inactiveColor,
					mainStyle);
			}
		}

		private Style SimpleStyle(string name,
			string property,
			Color color,
			bool value = true,
			params DataTrigger[] triggers)
		{
			var style = new Style(typeof(DataGridCell), baseStyle);
			StyleHelper.AddTriggers(style, property, value, color, activeColor, inactiveColor);
			foreach (var trigger in triggers)
				style.Triggers.Add(trigger);
			resources.Add(name, style);
			return style;
		}

		private Style CellStyle(Color active, Color inactive, string name, bool value, Color baseColor)
		{
			var baseStyle = (Style)Resources["VitallyImportant"];
			var style = new Style(typeof(DataGridCell), baseStyle);
			StyleHelper.AddTriggers(style, name, value, baseColor, active, inactive);
			return style;
		}

		private Style BaseStyle(Color active, Color inactive)
		{
			var baseStyle = (Style)Resources[typeof(DataGridCell)];
			var style = new Style(typeof(DataGridCell), baseStyle);
			StyleHelper.AddTriggers(style,
				"SortKeyGroup",
				1,
				Color.FromRgb(0xCC, 0xC1, 0xE3),
				active,
				inactive);
			StyleHelper.AddTriggers(style, "Banned", true, Colors.Red, active, inactive);

			style.Triggers.Add(VitallyImportant());

			return style;
		}

		public static DataTrigger VitallyImportant()
		{
			return new DataTrigger {
				Binding = new Binding("VitallyImportant"),
				Value = true,
				Setters = {
					new Setter(Control.ForegroundProperty, Brushes.Green)
				}
			};
		}
	}
}
