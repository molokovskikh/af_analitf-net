using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Helpers
{
	public class StyleHelper
	{
		public static Dictionary<string, DataTrigger> DefaultStyles = new Dictionary<string, DataTrigger>();
		public static Dictionary<string, SolidColorBrush> UserStyles = new Dictionary<string, SolidColorBrush>();

		private static ResourceDictionary localResources;

		public static SolidColorBrush DefaultColor = Brushes.Red;
		public static Color ActiveColor = Color.FromRgb(0xD7, 0xF0, 0xFF);
		public static Color InactiveColor = Color.FromRgb(0xDA, 0xDA, 0xDA);

		public static Dictionary<string, DataTrigger> BuildDefaultStyles()
		{
			return new Dictionary<string, DataTrigger> {
				{ "VitallyImportant", App.VitallyImportant() },
				{ "Frozen",
					new DataTrigger {
						Binding = new Binding("Frozen"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.Silver)
						}
					}
				},
				{ "DoNotHaveOffers",
					new DataTrigger {
						Binding = new Binding("DoNotHaveOffers"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.Silver)
						}
					}
				},
				{ "Marked",
					new DataTrigger {
						Binding = new Binding("Marked"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.Silver)
						}
					}
				},
				{ "NotBase",
					new DataTrigger {
						Binding = new Binding("NotBase"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)))
						}
					}
				},
				{ "HaveOrder",
					new DataTrigger {
						Binding = new Binding("HaveOrder"),
						Value = true,
						Setters = {
							new Setter(Control.FontWeightProperty, FontWeights.Bold)
						}
					}
				},
				{ "IsSendError",
					new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA4)))
						}
					}
				},
				{ "IsOrderLineSendError",
					new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA4)))
						}
					}
				},
				{ "IsCostDecreased",
					new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xb8, 0xff, 0x71)))
						}
					}
				},
				{ "IsCostIncreased",
					new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 82, 117)))
						}
					}
				},
				{ "IsQuantityChanged",
					new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 82, 117)))
						}
					}
				},
				{"SelectCount",
					new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF)))
						}
					}
				},
				{ "IsRejectChanged", Background("#ff8000") },
				{ "IsRejectNew", Background("#ff8000") },
				{ "IsRejectCanceled", Background("#808000") },
				{ "IsReject", Background("#a7ab9e") },
				{ "Junk", Background("#f29e66") },
				{ "Leader", Background("#C0DCC0") },
				{ "BeginOverlap", Background("#808000") },
				{ "HaveGap", Background("#800000") },
				{ "IsGrouped", Background("#CCC1E3") },
				{ "IsNotOrdered", Background("#FF8080") },
				{ "IsMinCost", Background("#ACFF97") },
				{ "ExistsInFreezed", Background("#C0C0C0") },
				{ "IsCreatedByUser", Background("#C0DCC0") },
			};
		}

		private static DataTrigger Background(string color)
		{
			return new DataTrigger {
				Setters = {
					new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)))
				}
			};
		}

		public static void Reset()
		{
			localResources = null;
			UserStyles.Clear();
			DefaultStyles.Clear();
		}

		public static void BuildStyles(ResourceDictionary app, IEnumerable<CustomStyle> styles = null)
		{
			if (DefaultStyles.Count == 0)
				DefaultStyles = BuildDefaultStyles();

			UserStyles = (styles ?? new CustomStyle[0] )
				.Where(s => s.IsBackground)
				.GroupBy(g => g.Name)
				.ToDictionary(s => s.Key, s => new SolidColorBrush((Color)ColorConverter.ConvertFromString(s.First().Background)));

			if (localResources == null) {
				localResources = new ResourceDictionary();
				app.MergedDictionaries.Add(localResources);
			}
			else {
				localResources.Clear();
			}

			var types = GetTypes();
			foreach (var type in types) {
				var mainStyle = type == typeof (WaybillLine)
					? (Style)app["DefaultEditableCell"]
					: (Style)app[typeof(DataGridCell)];
				BuildStyles(localResources, app,
					type,
					ActiveColor,
					InactiveColor,
					mainStyle);
			}
		}

		private static Type[] GetTypes()
		{
			var ignore = new[] { typeof(BaseOffer) };
			return typeof(StyleHelper).Assembly.GetTypes()
				.Except(ignore)
				.Where(t => t.GetProperties().Any(p => p.GetCustomAttributes(typeof(StyleAttribute), true).Length > 0))
				.ToArray();
		}

		public static void BuildStyles(
			ResourceDictionary local,
			ResourceDictionary app,
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
				let d = (StyleAttribute)a
				select new {type, p, d};

			var rowStyles = legends.Where(l => l.d.Columns == null || l.d.Columns.Length == 0);
			var rowStyle = new Style(typeof(DataGridCell), baseStyle);
			foreach (var style in rowStyles) {
				var property = style.p;
				var trigger = DefaultStyles.GetValueOrDefault(property.Name);
				if (trigger != null) {
					var background = trigger.Setters.OfType<Setter>()
						.FirstOrDefault(s => s.Property == Control.BackgroundProperty);
					if (background == null) {
						rowStyle.Triggers.Add(trigger);
					}
					else {
						AddBackgroundTriggers(rowStyle, property.Name);
					}
				}
				else {
					AddBackgroundTriggers(rowStyle, property.Name);
				}
			}

			if (rowStyle.Triggers.Count > 0) {
				local.Add(type.Name + "Row", rowStyle);
				baseStyle = rowStyle;
			}

			var cellStyles = legends.Where(l => !String.IsNullOrEmpty(l.d.Description));
			foreach (var legend in cellStyles) {
				var style = new Style(typeof(Label), (Style)app["Legend"]);
				var trigger = DefaultStyles.GetValueOrDefault(legend.p.Name);
				if (trigger == null) {
					style.Setters.Add(new Setter(Control.BackgroundProperty, DefaultColor));
				}
				else {
					trigger.Setters.OfType<Setter>()
						.Each(s => style.Setters.Add(new Setter(s.Property, s.Value, s.TargetName)));
				}
				style.Setters.Add(new Setter(ContentControl.ContentProperty, legend.d.Description));
				style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, legend.d.Description));
				local.Add(LegendKey(legend.type, legend.p), style);
			}

			foreach (var column in map) {
				var style = new Style(typeof(DataGridCell), baseStyle);
				string context = null;
				foreach (var property in column) {
					context = property.GetCustomAttributes(typeof(StyleAttribute), true)
						.Cast<StyleAttribute>()
						.Select(a => a.Context)
						.FirstOrDefault();

					var color = AddBackgroundTriggers(style, property.Name);
					GetValue(style, property.Name, true, color);
				}
				local.Add(CellKey(type, column.Key, context), style);
			}
		}

		private static SolidColorBrush GetColor(string key)
		{
			var baseColor = DefaultColor;
			var trigger = DefaultStyles.GetValueOrDefault(key);
			if (trigger != null) {
				var setter = trigger.Setters.OfType<Setter>()
					.FirstOrDefault(s => s.Property == Control.BackgroundProperty);
				if (setter != null) {
					baseColor = ((SolidColorBrush)setter.Value);
				}
			}
			baseColor = UserStyles.GetValueOrDefault(key) ?? baseColor;
			return baseColor;
		}

		public static string LegendKey(Type type, PropertyInfo property)
		{
			return type.Name + property.Name + "Legend";
		}

		private static string CellKey(Type type, string path, string context = null)
		{
			if (context == null)
				return type.Name + path + "Cell";
			else
				return context + type.Name + path + "Cell";
		}

		public static SolidColorBrush AddBackgroundTriggers(Style style, string name)
		{
			var brush = GetColor(name);
			AddTriggers(style, name, true, brush.Color, ActiveColor, InactiveColor);
			return brush;
		}

		public static void AddTriggers(Style style, string name, object value,
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
					new Condition(
						new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self },
						true),
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
					new Condition(
						new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self },
						false),
				},
				Setters = {
					new Setter(Control.BackgroundProperty, inactiveBrush),
					new Setter(Control.BorderBrushProperty, inactiveBrush)
				}
			};
			style.Triggers.Add(trigger);
		}


		public static void AddEditableTriggers(Style style, string name, object value,
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
					new Condition(
						new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self },
						true),
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
					new Condition(
						new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self },
						false),
				},
				Setters = {
					new Setter(Control.BackgroundProperty, inactiveBrush),
					new Setter(Control.BorderBrushProperty, inactiveBrush)
				}
			};
			style.Triggers.Add(trigger);
		}

		//для текущей ячейки когда в ней фокус рисуется прозрачный фон
		//а для подсвеченных ячеек должен рисоваться не смешанный фон
		private static void GetValue(Style style,
			string name,
			object value,
			SolidColorBrush normalBrush)
		{
			var trigger = new MultiDataTrigger {
				Conditions = {
					new Condition(new Binding(name), value),
					new Condition(new Binding("(IsFocused)") { RelativeSource = RelativeSource.Self }, true),
					new Condition(new Binding("(IsSelected)") { RelativeSource = RelativeSource.Self }, true),
					new Condition(
						new Binding("(Selector.IsSelectionActive)") { RelativeSource = RelativeSource.Self },
						true),
				},
				Setters = {
					new Setter(Control.BackgroundProperty, normalBrush),
					new Setter(Control.BorderBrushProperty, normalBrush)
				}
			};
			style.Triggers.Add(trigger);
		}

		public static Color Mix(Color background, Color foreground, float factor)
		{
			return (foreground - background) * factor + background;
		}

		public static void ApplyStyles(Type type, DataGrid grid, ResourceDictionary resources,
			Panel legend = null,
			string context = null)
		{
			foreach (var dataGridColumn in grid.Columns.OfType<DataGridBoundColumn>()) {
				var binding = dataGridColumn.Binding as Binding;
				if (binding == null)
					continue;
				var resource = resources[CellKey(type, binding.Path.Path)] as Style
					?? resources[CellKey(type, binding.Path.Path, context)] as Style;
				if (resource == null)
					continue;
				dataGridColumn.CellStyle = resource;
			}

			grid.CellStyle = (Style)resources[type.Name + "Row"];
			BuildLegend(type, grid, resources, legend, context);
		}

		private static void BuildLegend(Type type, DataGrid grid, ResourceDictionary resources, Panel legend,
			string context)
		{
			if (legend == null)
				return;

			var labels = from p in type.GetProperties()
				from StyleAttribute a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				where grid.Columns.OfType<DataGridBoundColumn>().Any(c => IsApplicable(c, a))
					&& (String.IsNullOrEmpty(a.Context) || context == a.Context)
				orderby a.Description
				let key = LegendKey(type, p)
				let style = resources[key] as Style
				where style != null
				select new Label { Style = style };

			if (legend.Children.Count == 0) {
				legend.Children.Add(new Label { Content = "Подсказка" });
				var stack = new WrapPanel();
				stack.Orientation = Orientation.Horizontal;
				stack.Children.AddRange(labels);
				legend.Children.Add(stack);
			}
			else {
				legend.Children.OfType<StackPanel>().First().Children.AddRange(labels);
			}
		}

		private static bool IsApplicable(DataGridBoundColumn col, StyleAttribute attr)
		{
			return attr.Columns.Length == 0 || (col.Binding != null &&  attr.Columns.Contains(((Binding)col.Binding).Path.Path));
		}

		public static List<CustomStyle> GetDefaultStyles()
		{
			var styles = GetTypes()
				.SelectMany(t => t.GetProperties().Select(p => Tuple.Create(p, p.GetCustomAttributes(typeof(StyleAttribute), true).OfType<StyleAttribute>().ToArray())))
				.SelectMany(t => t.Item2.Select(a => Tuple.Create(t.Item1, a)))
				.ToArray();

			return styles.Select(t => {
				var appStyle = new CustomStyle {
					Name = t.Item1.Name,
					Description = t.Item2.Description,
				};
				var trigger = DefaultStyles.GetValueOrDefault(t.Item1.Name)
					?? Background(DefaultColor.Color.ToString());
				var background = trigger.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.BackgroundProperty);
				var foreground = trigger.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.ForegroundProperty);
				if (background != null && background.Value is SolidColorBrush) {
					appStyle.Background = ((SolidColorBrush)background.Value).Color.ToString();
					appStyle.IsBackground = true;
				}
				else if (foreground != null && foreground.Value is SolidColorBrush) {
					appStyle.Foreground = ((SolidColorBrush)foreground.Value).Color.ToString();
				}
				return String.IsNullOrEmpty(appStyle.Description) ? null : appStyle;
			})
			.Where(s => s != null).ToList();
		}
	}
}