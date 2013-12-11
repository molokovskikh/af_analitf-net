using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

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
		}
	}
}