using System;
using System.Collections.Generic;
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

namespace AnalitF.Net.Client.Views
{
	/// <summary>
	/// Interaction logic for Debug.xaml
	/// </summary>
	public partial class Debug : UserControl
	{
		public Debug()
		{
			InitializeComponent();
			Loaded += (sender, args) => {
				Sql.Focus();
			};
		}

		private void Set1386x768(object sender, RoutedEventArgs e)
		{
			var mainWindow = Application.Current.MainWindow;
			mainWindow.WindowState = WindowState.Normal;
			mainWindow.Width = 1386;
			mainWindow.Height = 768;
		}

		private void Set800x600(object sender, RoutedEventArgs e)
		{
			var mainWindow = Application.Current.MainWindow;
			mainWindow.WindowState = WindowState.Normal;
			mainWindow.Width = 800;
			mainWindow.Height = 600;
		}
	}
}
