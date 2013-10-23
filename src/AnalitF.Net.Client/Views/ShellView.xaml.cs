using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

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

			EventManager.RegisterClassHandler(typeof(ShellView), Hyperlink.RequestNavigateEvent,
				new RoutedEventHandler(
					(sender, args) => {
						var url = ((RequestNavigateEventArgs)args).Uri.ToString();
						new OpenResult(url).Execute(new ActionExecutionContext());
					}));
#if !DEBUG
			Snoop.Visibility = Visibility.Collapsed;
			Collect.Visibility = Visibility.Collapsed;
			Debug_ErrorCount.Visibility = Visibility.Collapsed;
			ShowDebug.Visibility = Visibility.Collapsed;
#endif
		}
	}
}
