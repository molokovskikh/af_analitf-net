using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Models;
using NHibernate.Hql.Ast.ANTLR;

namespace AnalitF.Net.Client
{
	public partial class App : Application
	{
		private Color inactiveColor;
		private Color activeColor;
		private ResourceDictionary resources;
		private Style baseStyle;

		public bool Quiet;

		public void RegisterResources()
		{
			activeColor = Color.FromRgb(0xD7, 0xF0, 0xFF);
			inactiveColor = Color.FromRgb(0xDA, 0xDA, 0xDA);

			resources = Resources.MergedDictionaries[1];

			baseStyle = (Style)Resources[typeof(DataGridCell)];
			var style = new Style(typeof(DataGridCell), baseStyle);
			style.Triggers.Add(new DataTrigger {
				Binding = new Binding("VitallyImportant"),
				Value = true,
				Setters = {
					new Setter(Control.ForegroundProperty, Brushes.Green)
				}
			});
			resources.Add("BaseOrderLine", style);

			SimpleStyle("CatalogHaveOffers", "HaveOffers", Colors.Silver, false, VitallyImportant());
			SimpleStyle("MnnHaveOffers", "HaveOffers", Colors.Silver, false);

			style = new Style(typeof(DataGridCell), baseStyle);
			AddTriggers(style, "Junk", true, Color.FromRgb(0xf2, 0x9e, 0x66), activeColor, inactiveColor);
			resources.Add("JunkOrderLine", style);

			style = new Style(typeof(DataGridCell), baseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("CountColumn", style);

			SimpleStyle("BaseOrder", "Frozen", Colors.Silver);
			SimpleStyle("Reject", "Marked", Colors.Silver);

			resources.Add("VitallyImportant", BaseStyle(activeColor, inactiveColor));

			style = CellStyle(activeColor, inactiveColor, "Junk", true, Color.FromRgb(0xf2, 0x9e, 0x66));
			resources.Add("Junk", style);

			style = CellStyle(activeColor, inactiveColor, "HaveOffers", false, Colors.Silver);
			resources.Add("HaveOffers", style);

			style = CellStyle(activeColor, inactiveColor, "Leader", false, Color.FromRgb(0xC0, 0xDC, 0xC0));
			resources.Add("Leader", style);

			style = CellStyle(activeColor, inactiveColor, "Price.BasePrice", false, Color.FromRgb(0xF0, 0xF0, 0xF0));
			resources.Add("NotBaseOffer", style);

			style = new Style(typeof(DataGridCell), baseStyle);
			AddTriggers(style, "BeginOverlap", true, Color.FromRgb(0x80, 0x80, 0), activeColor, inactiveColor);
			AddTriggers(style, "HaveGap",  true, Color.FromRgb(0x80, 0, 0), activeColor, inactiveColor);
			resources.Add("BeginMarkup", style);

			SimpleStyle("EndMarkup", "EndLessThanBegin", Colors.Red);

			var offerBaseStyle = (Style)Resources["VitallyImportant"];
			style = new Style(typeof(DataGridCell), offerBaseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("OrderColumn", style);
			BuildStyles(resources, Resources, typeof(WaybillLine), activeColor, inactiveColor, baseStyle);
		}

		public static void BuildStyles(ResourceDictionary resources,
			ResourceDictionary appResource,
			Type type,
			Color activeColor,
			Color inactiveColor,
			Style baseStyle = null)
		{
			var map = (from p in type.GetProperties()
				from a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				from c in ((StyleAttribute)a).Columns
				group p by c into g
				select g);
			var legends = from p in type.GetProperties()
				from a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				let d = ((StyleAttribute)a).Description
				where !String.IsNullOrEmpty(d)
				select new {p, d};

			foreach (var legend in legends) {
				var style = new Style(typeof(Label), (Style)appResource["Legend"]);
				style.Setters.Add(new Setter(Label.BackgroundProperty, Brushes.Red));
				style.Setters.Add(new Setter(Label.ContentProperty, legend.d));
				resources.Add(LegendKey(legend.p), style);
			}

			foreach (var column in map) {
				var style = new Style(typeof(DataGridCell), baseStyle);
				foreach (var property in column) {
					AddTriggers(style, property.Name, true, Colors.Red, activeColor, inactiveColor);
				}
				resources.Add(type.Name + column.Key + "Cell", style);
			}
		}

		public static string LegendKey(PropertyInfo property)
		{
			return property.DeclaringType.Name + property.Name + "Legend";
		}

		private Style SimpleStyle(string name, string property, Color color, bool value = true, params DataTrigger[] triggers)
		{
			var style = new Style(typeof(DataGridCell), baseStyle);
			AddTriggers(style, property, value, color, activeColor, inactiveColor);
			foreach (var trigger in triggers)
				style.Triggers.Add(trigger);
			resources.Add(name, style);
			return style;
		}

		private Style CellStyle(Color active, Color inactive, string name, bool value, Color baseColor)
		{
			var baseStyle = (Style)Resources["VitallyImportant"];
			var style = new Style(typeof(DataGridCell), baseStyle);
			AddTriggers(style, name, value, baseColor, active, inactive);
			return style;
		}

		private Style BaseStyle(Color active, Color inactive)
		{
			var baseStyle = (Style)Resources[typeof(DataGridCell)];
			var style = new Style(typeof(DataGridCell), baseStyle);
			AddTriggers(style, "SortKeyGroup", 1, Color.FromRgb(0xCC, 0xC1, 0xE3), active, inactive);
			AddTriggers(style, "Banned", true, Colors.Red, active, inactive);

			style.Triggers.Add(VitallyImportant());

			return style;
		}

		private static DataTrigger VitallyImportant()
		{
			return new DataTrigger {
				Binding = new Binding("VitallyImportant"),
				Value = true,
				Setters = {
					new Setter(Control.ForegroundProperty, Brushes.Green)
				}
			};
		}

		private static void AddTriggers(Style style, string name, object value,
			Color baseColor,
			Color active,
			Color inactive)
		{
			var color = baseColor;
			var normalBrush = new SolidColorBrush(color);
			var activeBrush = new SolidColorBrush(Mix(color, active, 0.6f));
			var inactiveBrush = new SolidColorBrush(Mix(color, inactive, 0.6f));

			var trigger = new MultiDataTrigger {
					Conditions = {
							new Condition(new Binding(name), value),
							new Condition(new Binding("(IsSelected)") { RelativeSource = RelativeSource.Self }, false)
					},
					Setters = {
							new Setter(Control.BackgroundProperty, normalBrush),
							new Setter(Control.BorderBrushProperty, normalBrush)
					}
			};
			style.Triggers.Add(trigger);

			trigger = new MultiDataTrigger {
					Conditions = {
							new Condition(new Binding(name), value),
							new Condition(new Binding("(IsSelected)") { RelativeSource = RelativeSource.Self }, true),
							new Condition(new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self }, true),
					},
					Setters = {
							new Setter(Control.BackgroundProperty, activeBrush),
							new Setter(Control.BorderBrushProperty, activeBrush)
					}
			};
			style.Triggers.Add(trigger);

			trigger = new MultiDataTrigger {
					Conditions = {
							new Condition(new Binding(name), value),
							new Condition(new Binding("(IsSelected)") { RelativeSource = RelativeSource.Self }, true),
							new Condition(new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self }, false),
					},
					Setters = {
							new Setter(Control.BackgroundProperty, inactiveBrush),
							new Setter(Control.BorderBrushProperty, inactiveBrush)
					}
			};
			style.Triggers.Add(trigger);
		}

		public static Color Mix(Color background, Color foreground, float factor)
		{
			return (foreground - background) * factor + background;
		}
	}
}
