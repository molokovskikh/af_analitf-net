using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AnalitF.Net.Client
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			RegisterResources();
		}

		public void RegisterResources()
		{
			var activeColor = Color.FromRgb(0xD7, 0xF0, 0xFF);
			var inactiveColor = Color.FromRgb(0xDA, 0xDA, 0xDA);

			var style = CellStyle(activeColor, inactiveColor, "Junk", true, Color.FromRgb(0xf2, 0x9e, 0x66));
			Resources.MergedDictionaries[1].Add("Junk", style);

			style = CellStyle(activeColor, inactiveColor, "HaveOffers", false, Colors.Silver);
			Resources.MergedDictionaries[1].Add("HaveOffers", style);

			style = CellStyle(activeColor, inactiveColor, "Leader", false, Color.FromRgb(0xC0, 0xDC, 0xC0));
			Resources.MergedDictionaries[1].Add("Leader", style);
		}

		private Style CellStyle(Color active, Color inactive, string name, bool value, Color baseColor)
		{
			var color = baseColor;
			var normalBrush = new SolidColorBrush(color);
			var activeBrush = new SolidColorBrush(Mix(color, active, 0.6f));
			var inactiveBrush = new SolidColorBrush(Mix(color, inactive, 0.6f));

			var baseStyle = (Style)Resources["VitallyImportant"];
			var style = new Style(typeof(DataGridCell)) {
				BasedOn = baseStyle,
				Triggers = {
					new MultiDataTrigger {
						Conditions = {
							new Condition(new Binding(name), value),
							new Condition(new Binding("IsSelected") { RelativeSource = RelativeSource.Self }, false)
						},
						Setters = {
							new Setter(Control.BackgroundProperty, normalBrush),
							new Setter(Control.BorderBrushProperty, normalBrush)
						}
					},
					new MultiDataTrigger {
						Conditions = {
							new Condition(new Binding(name), value),
							new Condition(new Binding("IsSelected") { RelativeSource = RelativeSource.Self }, true),
							new Condition(new Binding("Selector.IsSelectionActive") { RelativeSource = RelativeSource.Self }, true),
						},
						Setters = {
							new Setter(Control.BackgroundProperty, activeBrush),
							new Setter(Control.BorderBrushProperty, activeBrush)
						}
					},
					new MultiDataTrigger {
						Conditions = {
							new Condition(new Binding(name), value),
							new Condition(new Binding("IsSelected") { RelativeSource = RelativeSource.Self }, true),
							new Condition(new Binding("Selector.IsSelectionActive") { RelativeSource = RelativeSource.Self }, false),
						},
						Setters = {
							new Setter(Control.BackgroundProperty, inactiveBrush),
							new Setter(Control.BorderBrushProperty, inactiveBrush)
						}
					}
				}
			};
			return style;
		}

		public Color Mix(Color background, Color foreground, float factor)
		{
			return (foreground - background) * factor + background;
		}
	}
}
