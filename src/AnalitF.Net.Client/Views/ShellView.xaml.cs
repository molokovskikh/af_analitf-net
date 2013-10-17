using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class ShellView : Window
	{
		public ShellView()
		{
			InitializeComponent();
			Loaded += (sender, args) => {
				//для тестов
				var button = Update.Descendants<Button>().First(b => b.Name == "PART_ActionButton");
				button.SetValue(AutomationProperties.AutomationIdProperty, "Update");
			};
#if !DEBUG
			Snoop.Visibility = Visibility.Collapsed;
			Collect.Visibility = Visibility.Collapsed;
			Debug_ErrorCount.Visibility = Visibility.Collapsed;
			ShowDebug.Visibility = Visibility.Collapsed;
#endif
		}
	}
}
