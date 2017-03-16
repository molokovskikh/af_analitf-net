using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Iesi.Collections;
using Newtonsoft.Json.Utilities;
using NPOI.SS.Formula.Functions;
using WinForm = System.Windows.Forms;

namespace AnalitF.Net.Client.Helpers
{
	public class StyleAttribute : Attribute
	{
		public string[] Columns;
		public string Description;
		public string Context;
		/// <summary>
		/// приоритет стиля, для стиля ячейки не делает ничего
		/// для стиля строки
		/// если меньше 0 - стиль применяет до стиля ячейки и может быть переписан стилем ячейки
		/// если больше или равно 0 - стиль применяет после стилей ячейки и может переписать стиль который задает ячейка
		/// все стили применяются отсортированные от меньшего к большему
		/// по умолчанию -1
		/// </summary>
		public int Priority = -1;
		/// <summary>
		/// Название стиля который должен применяться, в большинстве случаем null
		/// нужен в тех ситуация когда название стиля должно отличаться от названия свойства
		/// например два класса должны использовать один стиль что бы настройки применялись идентичные
		/// но название свойства не могут быть одинаковыми тк в одном из классов такое свойство уже используется
		/// а изменение имени существующего свойства трудоемко
		/// </summary>
		public string Name;

		public StyleAttribute(params string[] columns)
		{
			Columns = columns;
		}

		public string GetName(PropertyInfo property)
		{
			return Name ?? property.Name;
		}
	}

	public class StyleHelper
	{
		public static Dictionary<string, DataTrigger> DefaultStyles = new Dictionary<string, DataTrigger>();
		public static Dictionary<string, Setter> UserStyles = new Dictionary<string, Setter>();

		private static ResourceDictionary localResources;

		public static SolidColorBrush DefaultColor = Brushes.Red;
		public static Color ActiveColor = Color.FromRgb(0xD7, 0xF0, 0xFF);
		public static Color InactiveColor = Color.FromRgb(0xDA, 0xDA, 0xDA);

		//в каталоге людям не нравится бледное выделение, нужно использовать обычное
		//но в этом случае смешение цветов работает плохо из-за того что цвет текста становится не контрастным
		//и он плохо читается, по хорошему нужно вычислять контрастный цвет текста но это банка червей
		//по этому для этих стилей смешение не используется
		public static Dictionary<Type, Tuple<Color, Color, string>> BaseStyles
			= new Dictionary<Type, Tuple<Color, Color, string>> {
				{typeof(CatalogName), Tuple.Create(SystemColors.HighlightColor, SystemColors.HighlightColor, "Normal") },
				{typeof(Catalog), Tuple.Create(SystemColors.HighlightColor, SystemColors.HighlightColor, "Normal") },
			};

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
				{ "IsImported",
					new DataTrigger {
						Binding = new Binding("IsImported"),
						Value = true,
						Setters = {
							new Setter(Control.BackgroundProperty, Brushes.LightCoral)
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
							new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEF, 0x52, 0x75)))
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
				{ "IsNotPosted", Background(Colors.PaleTurquoise) },
				{ "IsCertificateNotFound", Background(Colors.Gray) },
				{ "OrderMark", Background(Color.FromRgb(0xEE, 0xF8, 0xFF)) },
				{ "IsUnmatchedByWaybill", Background(Color.FromRgb(248, 238, 141)) },
				{ "IsLimited", Background(Color.FromRgb(255, 96, 0)) },
				{ "IsSplitByLimit", Background(Color.FromRgb(255, 191, 0)) },
				{ "IsCurrentAddress",
					new DataTrigger {
						Binding = new Binding("IsCurrentAddress"),
						Value = true,
						Setters = {
							new Setter(Control.FontWeightProperty, FontWeights.Bold)
						}
					}
				},
				{ "IsNew",
					new DataTrigger {
						Binding = new Binding("IsNew"),
						Value = true,
						Setters = {
							new Setter(Control.FontWeightProperty, FontWeights.Bold)
						}
					}
				},
				{ "IsFullyStocked", Background("#25BC37") },
				{ "IsPartialyStocked", Background("#94BA99") },
				{ "IsRetailCostFixed", Background(Color.FromRgb(250, 155, 250)) },
				{ "IsOverdue", new DataTrigger {
						Binding = new Binding("IsOverdue"),
						Value = true,
						Setters = {
							new Setter(Control.ForegroundProperty, new SolidColorBrush(Colors.Red)),
						}
					}
				},
				{ "InFrozenOrders", Background(Colors.Gray) },
				{ "Confirmed", Background(Colors.Green) },
			};
		}

		private static DataTrigger Background(Color color)
		{
			return new DataTrigger {
				Setters = {
					new Setter(Control.BackgroundProperty, new SolidColorBrush(color))
				}
			};
		}

		private static DataTrigger Background(string color)
		{
			return Background((Color)ColorConverter.ConvertFromString(color));
		}

		public static void Reset()
		{
			localResources = null;
			UserStyles.Clear();
			DefaultStyles = BuildDefaultStyles();
		}

		public static void BuildStyles(ResourceDictionary app, IEnumerable<CustomStyle> styles = null)
		{
			if (DefaultStyles.Count == 0)
				DefaultStyles = BuildDefaultStyles();

			UserStyles = (styles ?? new CustomStyle[0] )
				.GroupBy(g => g.Name)
				.ToDictionary(s => s.Key, s => s.First().ToSetter());

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
				var activeColor = ActiveColor;
				var inactiveColor = InactiveColor;
				var baseStyle = BaseStyles.GetValueOrDefault(type);
				var mixColors = true;
				if (baseStyle != null) {
					activeColor = baseStyle.Item1;
					inactiveColor = baseStyle.Item2;
					mainStyle = (Style)app[baseStyle.Item3];
					mixColors = false;
				}
				BuildStyles(localResources, app, type, activeColor, inactiveColor, mainStyle, mixColors);
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
			Style baseStyle = null,
			bool mixColors = true)
		{
			var styles = from p in type.GetProperties()
				from a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				let d = (StyleAttribute)a
				select Tuple.Create(type, p, d);
			var rowStyles = styles.Where(l => l.Item3.Columns == null || l.Item3.Columns.Length == 0).ToArray();
			var rowStyle = new Style(typeof(DataGridCell), baseStyle);
			ApplyToStyle(rowStyles.OrderBy(s => s.Item3.Priority), rowStyle, mixColors);

			if (rowStyle.Triggers.Count > 0) {
				local.Add(type.Name + "Row", rowStyle);
				baseStyle = rowStyle;
			}

			var cellStyles = styles.Where(l => !String.IsNullOrEmpty(l.Item3.Description));
			foreach (var legend in cellStyles) {
				var style = new Style(typeof(Label), (Style)app["Legend"]);
				GetCombinedTrigger(legend.Item3.GetName(legend.Item2), legend.Item1.Name)
					.Setters
					.OfType<Setter>()
					.Each(s => style.Setters.Add(new Setter(s.Property, s.Value, s.TargetName)));
				style.Setters.Add(new Setter(ContentControl.ContentProperty, legend.Item3.Description));
				style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, legend.Item3.Description));
				local.Add(LegendKey(legend.Item1, legend.Item2), style);
			}

			var columnStyles = (from s in styles
				from c in s.Item3.Columns
				group s by c into g
				select g);
			foreach (var column in columnStyles) {
				var style = new Style(typeof(DataGridCell), baseStyle);
				string context = null;

				//низко-приоритетные стили строки
				ApplyToStyle(rowStyles.Where(s => s.Item3.Priority < 0).OrderBy(s => s.Item3.Priority), style, mixColors);

				foreach (var styleDef in column) {
					var prop = styleDef.Item2;
					var attr = styleDef.Item3;
					context = attr.Context;

					var key = attr.GetName(prop);
					var brush = GetColor(key);
					if (brush != null) {
						AddMixedBackgroundTriggers(style, prop.Name, true, brush.Color, ActiveColor, InactiveColor);
						AddFocusedTrigger(style, prop.Name, true, brush);
					}
					else {
						style.Triggers.Add(DefaultStyles.GetValueOrDefault(key));
					}
				}

				//высоко-приоритетные стили строки
				ApplyToStyle(rowStyles.Where(s => s.Item3.Priority >= 0).OrderBy(s => s.Item3.Priority), style, mixColors);

				style.Seal();
				local.Add(CellKey(type, column.Key, context), style);
			}
		}

		private static void ApplyToStyle(IEnumerable<Tuple<Type, PropertyInfo, StyleAttribute>> styles, Style rowStyle, bool mixColors)
		{
			foreach (var style in styles) {
				var property = style.Item2.Name;
				var trigger = GetCombinedTrigger(style.Item3.GetName(style.Item2), style.Item2.Name);
				var background = trigger.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.BackgroundProperty);
				if (background == null) {
					rowStyle.Triggers.Add(trigger);
					continue;
				}

				trigger.Setters.Remove(background);
				if (mixColors)
					AddMixedBackgroundTriggers(rowStyle, property, true, ((SolidColorBrush)background.Value).Color, ActiveColor, InactiveColor);
				else
					AddBackgroundTriggers(rowStyle, property, true, (Brush)background.Value);
				if (trigger.Setters.Count > 0)
					rowStyle.Triggers.Add(trigger);
			}
		}

		private static void AddBackgroundTriggers(Style style, string name, bool value, Brush brush)
		{
			var trigger = new MultiDataTrigger {
				Conditions = {
					new Condition(new Binding(name), value),
					new Condition(new Binding("(IsSelected)") { RelativeSource = RelativeSource.Self }, false)
				},
				Setters = {
					new Setter(Control.BackgroundProperty, brush),
					new Setter(Control.BorderBrushProperty, brush)
				}
			};
			style.Triggers.Add(trigger);
		}

		private static DataTrigger GetCombinedTrigger(string key, string property)
		{
			//PatchBackground модифицирует триггер по этому копируем
			var trigger = Copy(DefaultStyles.GetValueOrDefault(key))
				?? new DataTrigger {
					Setters = {
						new Setter(Control.BackgroundProperty, DefaultColor)
					}
				};
			var userStyle = UserStyles.GetValueOrDefault(key);
			if (userStyle != null) {
				var copy = new DataTrigger();
				trigger.Setters.OfType<Setter>()
					.Where(s => s.Property != userStyle.Property)
					.Each(s => copy.Setters.Add(new Setter(s.Property, s.Value)));
				copy.Setters.Add(userStyle);
				trigger = copy;
			}
			trigger.Binding = new Binding(property);
			trigger.Value = true;
			return trigger;
		}

		private static DataTrigger Copy(DataTrigger src)
		{
			if (src == null)
				return null;
			var copy = new DataTrigger();
			src.Setters.OfType<Setter>().Each(s => copy.Setters.Add(new Setter(s.Property, s.Value)));
			return copy;
		}

		private static SolidColorBrush GetColor(string key)
		{
			var baseColor = DefaultColor;
			var trigger = DefaultStyles.GetValueOrDefault(key);
			if (trigger != null) {
				var setter = trigger.Setters.OfType<Setter>()
					.FirstOrDefault(s => s.Property == Control.BackgroundProperty);
				//если стиль определен но он не затрагивает цвет фона
				if (setter == null)
					return null;
				baseColor = ((SolidColorBrush)setter.Value);
			}
			var userStyle = UserStyles.GetValueOrDefault(key);
			if (userStyle != null && userStyle.Property == Control.BackgroundProperty) {
				baseColor = (SolidColorBrush)userStyle.Value;
			}
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

		public static void AddMixedBackgroundTriggers(Style style, string name, object value,
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
		private static void AddFocusedTrigger(Style style,
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

		public static void ApplyStyles(Type type, UIElement grid, ResourceDictionary resources,
			Panel legend = null,
			string context = null)
		{
			if (grid is DataGrid)
			{
				foreach (var column in (grid as DataGrid).Columns)
				{
					var key = GetKey(column);
					if (String.IsNullOrEmpty(key))
						continue;
					var resource = resources[CellKey(type, key)] as Style
						?? resources[CellKey(type, key, context)] as Style;
					if (resource == null)
						continue;
					column.CellStyle = resource;
				}

				 (grid as DataGrid).CellStyle = (Style)resources[type.Name + "Row"];
			}
			BuildLegend(type, grid, resources, legend, context);
		}

		private static string GetKey(DataGridColumn column)
		{
			var key = column.GetValue(FrameworkElement.NameProperty) as string;
			var boundColumn = column as DataGridBoundColumn;
			if (boundColumn != null) {
				var binding = boundColumn.Binding as Binding;
				if (binding != null) {
					key = binding.Path.Path;
				}
			}
			return key;
		}

		private static string GetKey(WinForm.DataGridViewColumn column)
		{
			var key = column.DataPropertyName;
			return key;
		}

		private static void BuildLegend(Type type, UIElement grid, ResourceDictionary resources, Panel legend,
			string context)
		{
			if (legend == null)
				return;

			var labels = from p in type.GetProperties()
				from StyleAttribute a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				where  ((grid is DataGrid && (grid as DataGrid).Columns.Any(c => IsApplicable(c, a)))
							 || (grid is WinFormDataGrid && IsApplicable((WinFormDataGrid)grid, a)))
					&& (String.IsNullOrEmpty(a.Context) || context == a.Context)
				orderby a.Description
				let key = LegendKey(type, p)
				let style = resources[key] as Style
				where style != null
				select ConnectEdit(new Label {
					Style = style,
					Tag = "generated" + (grid as FrameworkElement).Name,
					Name = a.GetName(p) + "LegendItem",
				});

			if (legend.Children.Count == 0) {
				var header = new StackPanel {
					Orientation = Orientation.Horizontal
				};
				header.Children.Add(new Label {
					Content = "Подсказка",
					Padding = new Thickness(5, 0, 5, 0),
					VerticalAlignment = VerticalAlignment.Center
				});
				var toggleButton = new ToggleButton {
					Content = "+",
					ToolTip = "Свернуть\\развернуть",
					Padding = new Thickness(5, 0, 5, 0),
					Margin = new Thickness(0),
					Background = Brushes.Transparent,
					BorderBrush = Brushes.Transparent,
					Focusable = false,
				};
				header.Children.Add(toggleButton);
				legend.Children.Add(header);

				var stack = new LegendPanel();
				stack.BindTo<bool>("IsOverflow",
					toggleButton, ToggleButton.VisibilityProperty, x => x ? Visibility.Visible : Visibility.Collapsed);
				stack.BindTo<bool>("IsCollapsed",
					toggleButton, ToggleButton.ContentProperty, x => x ? "+" : "-");
				toggleButton.BindTo<bool?>("IsChecked",
					stack, LegendPanel.IsCollapsedProperty, x => !x.GetValueOrDefault());

				stack.Children.AddRange(labels);
				legend.Children.Add(stack);
			}
			else {
				//если пользовательские стили изменились нужно перестроить легенду
				var panel = legend.Children.OfType<LegendPanel>().First();
				panel.Children.OfType<FrameworkElement>().Where(c => Equals("generated" + (grid as FrameworkElement).Name, c.Tag))
					.ToArray()
					.Each(c => panel.Children.Remove(c));
				panel.Children.AddRange(labels);
				if (grid is WinFormDataGrid)
					(grid as WinFormDataGrid).IsStyleAppled = true;
			}
		}

		private static UIElement ConnectEdit(Label label)
		{
			label.MouseDoubleClick += (sender, args) => {
				var screen = label.DataContext as BaseScreen;
				if (screen != null)
					ViewModelHelper.ProcessResult(screen.EditLegend(label.Name.Replace("LegendItem", "")));
			};
			return label;
		}

		private static bool IsApplicable(DataGridColumn col, StyleAttribute attr)
		{
			var key = GetKey(col);
			return attr.Columns.Length == 0 || (!String.IsNullOrEmpty(key) && attr.Columns.Contains(key));
		}

		private static bool IsApplicable(WinFormDataGrid grid, StyleAttribute attr)
		{
			foreach (WinForm.DataGridViewColumn col in grid.DataGrid.Columns)
			{
				var key = GetKey(col);
				if (attr.Columns.Length == 0 || (!String.IsNullOrEmpty(key) && attr.Columns.Contains(key)))
					return true;
			}
			return false;
		}

		public static List<CustomStyle> GetDefaultStyles()
		{
			return GetDefaultStyles(DefaultStyles);
		}

		public static List<CustomStyle> GetDefaultStyles(Dictionary<string, DataTrigger> defaults)
		{
			var styles = GetTypes()
				.SelectMany(t => t.GetProperties().Select(p => Tuple.Create(p, p.GetCustomAttributes(typeof(StyleAttribute), true).OfType<StyleAttribute>().ToArray())))
				.SelectMany(t => t.Item2.Select(a => Tuple.Create(t.Item1, a)))
				.ToArray();

			return styles.Select(t => {
				var appStyle = new CustomStyle {
					Name = t.Item2.GetName(t.Item1),
					Description = t.Item2.Description,
				};
				var trigger = defaults.GetValueOrDefault(appStyle.Name)
					?? Background(DefaultColor.Color);
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