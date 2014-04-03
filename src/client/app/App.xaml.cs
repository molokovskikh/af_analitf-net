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
		private ResourceDictionary resources;
		private Style baseStyle;

		public SplashScreen Splash;
		public bool FaultInject;

		public void RegisterResources()
		{
			resources = Resources.MergedDictionaries[1];

			baseStyle = (Style)Resources[typeof(DataGridCell)];

			var style = new Style(typeof(DataGridCell), baseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty,
				new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("CountColumn", style);
			resources.Add("VitallyImportant", BaseStyle());

			style = CellStyle(StyleHelper.ActiveColor,
				StyleHelper.InactiveColor,
				"Price.BasePrice",
				false,
				Color.FromRgb(0xF0, 0xF0, 0xF0));
			resources.Add("NotBaseOffer", style);

			var offerBaseStyle = (Style)Resources["VitallyImportant"];
			style = new Style(typeof(DataGridCell), offerBaseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty,
				new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("OrderColumn", style);
			StyleHelper.BuildStyles(Resources);
		}

		private Style CellStyle(Color active, Color inactive, string name, bool value, Color baseColor)
		{
			var baseStyle = (Style)Resources["VitallyImportant"];
			var style = new Style(typeof(DataGridCell), baseStyle);
			StyleHelper.AddTriggers(style, name, value, baseColor, active, inactive);
			return style;
		}

		private Style BaseStyle()
		{
			var baseStyle = (Style)Resources[typeof(DataGridCell)];
			var style = new Style(typeof(DataGridCell), baseStyle);
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
