using System;
using System.Reflection;
using System.Windows;

namespace AnalitF.Net.Client.Views
{
	public partial class ShellView : Window
	{
		public ShellView()
		{
			InitializeComponent();
#if !DEBUG
			Snoop.Visibility = Visibility.Collapsed;
#endif
		}
	}
}
