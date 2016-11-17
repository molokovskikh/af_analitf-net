using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using ReactiveUI;

namespace AnalitF.Net.Client.Views
{
	public partial class SettingsView : UserControl
	{
		public SettingsView()
		{
			InitializeComponent();

			StyleHelper.ApplyStyles(typeof(MarkupConfig), Markups, Application.Current.Resources);
			StyleHelper.ApplyStyles(typeof(MarkupConfig), VitallyImportantMarkups, Application.Current.Resources);
#if !DEBUG
			DebugTab.Visibility = Visibility.Collapsed;
#endif

			DataContextChanged += (sender, args) => {
				var model = (SettingsViewModel)DataContext;

				model.ObservableForProperty(x => x.Settings.Value.PriceTag.Type, skipInitial: false)
					.Subscribe(x => {
						PriceTagConfig.Visibility = x.Value == PriceTagType.Custom ? Visibility.Collapsed : Visibility.Visible;
						PriceTagConstructor.Visibility = x.Value == PriceTagType.Custom ? Visibility.Visible : Visibility.Collapsed;
					});

				model.ObservableForProperty(x => x.Settings.Value.RackingMap.Size, skipInitial: false)
					.Subscribe(x => {
						RackingMapConfig.Visibility = x.Value == RackingMapSize.Custom ? Visibility.Collapsed : Visibility.Visible;
						RackingMapConstructor.Visibility = x.Value == RackingMapSize.Custom ? Visibility.Visible : Visibility.Collapsed;
					});
			};

			Settings_PriceTag_Type.SelectionChanged += (sender, e) =>
			{
				if ((PriceTagType)((ValueDescription)(sender as ComboBox).SelectedItem).Value == PriceTagType.Custom)
				{
					(sender as ComboBox).ToolTip = @"Конструктор для редактирования ценников, с учетом нужных размеров и необходимых опций. Для создания нового ценника нажмите Редактировать.";
					return;
				}

				(sender as ComboBox).ToolTip = null;
			};

			Settings_RackingMap_Size.SelectionChanged += (sender, e) =>
			{
				if ((RackingMapSize)((ValueDescription)(sender as ComboBox).SelectedItem).Value == RackingMapSize.Custom)
				{
					(sender as ComboBox).ToolTip = @"Конструктор для редактирования стеллажных карт, с учетом нужных размеров и необходимых опций. Для создания нового стеллажной карты нажмите Редактировать.";
					return;
				}

				(sender as ComboBox).ToolTip = null;
			};
		}
	}
}