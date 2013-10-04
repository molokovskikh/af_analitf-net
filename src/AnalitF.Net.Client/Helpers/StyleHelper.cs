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
		public static Dictionary<string, Func<DataTrigger>> KnownStyles
			= new Dictionary<string, Func<DataTrigger>> {
				{ "VitallyImportant", App.VitallyImportant },
				{ "Frozen",
					() => new DataTrigger {
						Binding = new Binding("Frozen"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.Silver)
						}
					}
				},
				{ "DoNotHaveOffers",
					() => new DataTrigger {
						Binding = new Binding("DoNotHaveOffers"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.Silver)
						}
					}
				},
				{ "Marked",
					() => new DataTrigger {
						Binding = new Binding("Marked"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.Silver)
						}
					}
				},
				{ "Junk",
					() => new DataTrigger {
						Binding = new Binding("Junk"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xf2, 0x9e, 0x66)))
						}
					}
				},
				{ "NotBase",
					() => new DataTrigger {
						Binding = new Binding("NotBase"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)))
						}
					}
				},
				{ "HaveOrder",
					() => new DataTrigger {
						Binding = new Binding("HaveOrder"),
						Value = true,
						Setters = {
							new Setter(Control.FontWeightProperty, FontWeights.Bold)
						}
					}
				},
				{ "IsSendError",
					() => new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA4)))
						}
					}
				},
				{ "IsOrderLineSendError",
					() => new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA4)))
						}
					}
				},
				{ "IsCostDecreased",
					() => new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xb8, 0xff, 0x71)))
						}
					}
				},
				{ "IsCostIncreased",
					() => new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 82, 117)))
						}
					}
				},
				{ "IsQuantityChanged",
					() => new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 82, 117)))
						}
					}
				},
				{"SelectCount",
					() => new DataTrigger {
						Setters = {
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF)))
						}
					}
				}
			};

		private static SolidColorBrush DefaultColor = Brushes.Red;

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
				let d = (StyleAttribute)a
				select new {type, p, d};

			var rowStyles = legends.Where(l => l.d.Columns == null || l.d.Columns.Length == 0);
			var rowStyle = new Style(typeof(DataGridCell), baseStyle);
			foreach (var style in rowStyles) {
				var property = style.p;
				var result = KnownStyles.GetValueOrDefault(property.Name);
				if (result != null) {
					var trigger = result();
					var background = trigger.Setters.OfType<Setter>()
						.FirstOrDefault(s => s.Property == Control.BackgroundProperty);
					if (background == null) {
						rowStyle.Triggers.Add(trigger);
					}
					else {
						var color = ((SolidColorBrush)background.Value);
						AddTriggers(rowStyle, property.Name, true, color, activeColor, inactiveColor);
					}

				}
				else {
					AddTriggers(rowStyle, property.Name, true, DefaultColor, activeColor, inactiveColor);
				}
			}

			if (rowStyle.Triggers.Count > 0) {
				resources.Add(type.Name + "Row", rowStyle);
				baseStyle = rowStyle;
			}

			var cellStyles = legends.Where(l => !String.IsNullOrEmpty(l.d.Description));
			foreach (var legend in cellStyles) {
				var style = new Style(typeof(Label), (Style)appResource["Legend"]);
				var result = KnownStyles.GetValueOrDefault(legend.p.Name);
				if (result == null) {
					style.Setters.Add(new Setter(Control.BackgroundProperty, DefaultColor));
				}
				else {
					var trigger = result();
					trigger.Setters.OfType<Setter>()
						.Each(s => style.Setters.Add(new Setter(s.Property, s.Value, s.TargetName)));
				}
				style.Setters.Add(new Setter(ContentControl.ContentProperty, legend.d.Description));
				style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, legend.d.Description));
				resources.Add(LegendKey(legend.type, legend.p), style);
			}

			foreach (var column in map) {
				var style = new Style(typeof(DataGridCell), baseStyle);
				string context = null;
				foreach (var property in column) {
					context = property.GetCustomAttributes(typeof(StyleAttribute), true)
						.Cast<StyleAttribute>()
						.Select(a => a.Context)
						.FirstOrDefault();

					var baseColor = DefaultColor;
					var result = KnownStyles.GetValueOrDefault(property.Name);
					if (result != null) {
						var setter = result().Setters.OfType<Setter>()
							.FirstOrDefault(s => s.Property == Control.BackgroundProperty);
						if (setter != null) {
							baseColor = ((SolidColorBrush)setter.Value);
						}
					}
					AddTriggers(style, property.Name, true, baseColor, activeColor, inactiveColor);
					GetValue(style, property.Name, true, baseColor);
				}
				resources.Add(CellKey(type, column.Key, context), style);
			}
		}

		private static void Apply(Type type, DataGrid grid, ResourceDictionary resource)
		{
			grid.CellStyle = (Style)resource[type.Name + "Row"];
		}

		public static string LegendKey(Type type, PropertyInfo property)
		{
			return type.Name + property.Name + "Legend";
		}
		public static void AddTriggers(Style style, string name, object value,
			SolidColorBrush baseColor,
			Color active,
			Color inactive)
		{
			AddTriggers(style, name, value, baseColor.Color, active, inactive);
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
					new Condition(new Binding(
						"(IsSelected)") { RelativeSource = RelativeSource.Self }, true),
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

		public static void ApplyStyles(Type type, Controls.DataGrid grid, ResourceDictionary resources,
			StackPanel legend = null,
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

			Apply(type, grid, resources);

			BuildLegend(type, resources, legend);
		}

		private static string CellKey(Type type, string path, string context = null)
		{
			if (context == null)
				return type.Name + path + "Cell";
			else
				return context + type.Name + path + "Cell";
		}

		private static void BuildLegend(Type type, ResourceDictionary resources, StackPanel legend)
		{
			if (legend == null)
				return;

			legend.Children.Add(new Label { Content = "Подсказка" });
			var styles = from p in type.GetProperties()
				from a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				let key = LegendKey(type, p)
				let style = resources[key] as Style
				where style != null
				select new Label { Style = style };
			var stack = new StackPanel();
			stack.Orientation = Orientation.Horizontal;
			stack.Children.AddRange(styles);
			legend.Children.Add(stack);
		}
	}
}