using System;
using System.Collections.Generic;
using System.Data;
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
using AnalitF.Net.Client.Config;
using Devart.Data.MySql;

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

		private async void SQL_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) > 0) {
				e.Handled = true;
				SQL.IsEnabled = false;
				try {
					SqlError.Text = "Loading...";
					var sql = SQL.Text;
					Output.ItemsSource = await Env.Current.Query(s => {
						var adapter = new MySqlDataAdapter(sql, (MySqlConnection)s.Connection);
						var datatable = new DataTable();
						adapter.Fill(datatable);
						return datatable.DefaultView;
					});
					SqlError.Text = "";
				} catch(Exception ex) {
					SqlError.Text = ex.ToString();
				}
				SQL.IsEnabled = true;
			}
		}
	}
}
