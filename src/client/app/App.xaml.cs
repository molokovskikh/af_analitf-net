﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client
{
	public partial class App : Application
	{
		private ResourceDictionary resources;
		private Style baseStyle;

		public SplashScreen Splash;
		public bool FaultInject;

		public App()
		{
			//клиенты жалуются что при настройках по умолчанию текст "размыт"
			TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(Window),
				new FrameworkPropertyMetadata(TextFormattingMode.Display,
					FrameworkPropertyMetadataOptions.AffectsMeasure
						| FrameworkPropertyMetadataOptions.AffectsRender
						| FrameworkPropertyMetadataOptions.Inherits));
		}

		public void RegisterResources()
		{
			resources = Resources.MergedDictionaries[1];
			baseStyle = (Style)Resources[typeof(DataGridCell)];
			var style = new Style(typeof(DataGridCell), baseStyle);
			style.Setters.Add(new Setter(Control.BackgroundProperty,
				new SolidColorBrush(Color.FromRgb(0xEE, 0xF8, 0xFF))));
			resources.Add("CountColumn", style);
			resources.Add("VitallyImportant", BaseStyle());
			StyleHelper.BuildStyles(Resources);
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
