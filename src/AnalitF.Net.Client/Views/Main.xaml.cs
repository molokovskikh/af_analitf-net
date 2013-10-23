using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class Main : UserControl
	{
		public Main()
		{
			InitializeComponent();
			Helpers.DataGridHelper.CalculateColumnWidth(Newses, "0000.00.00", "Дата");

			var textEffect = new TextEffect();
			var smallWidth = Mesure(SmallLabel);
			var factor = Mesure(EtalogLabel) / smallWidth;
			textEffect.Transform = new ScaleTransform {
				CenterX = 0,
				ScaleX = factor
			};

			textEffect.PositionCount = int.MaxValue;
			SmallLabel.TextEffects.Clear();
			SmallLabel.TextEffects.Add(textEffect);
		}

		private double Mesure(TextBlock control)
		{
			var formattedText = new FormattedText(
				control.Text,
				CultureInfo.CurrentUICulture,
				control.FlowDirection,
				new Typeface(control.FontFamily,
					control.FontStyle,
					control.FontWeight,
					control.FontStretch),
					control.FontSize,
					control.Foreground);
			return formattedText.Width;
		}
	}
}
