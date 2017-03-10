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
using System.Windows.Threading;

namespace AnalitF.Net.Client.Views
{
	/// <summary>
	/// Interaction logic for SelectFromCatalog.xaml
	/// </summary>
	public partial class SelectFromCatalog : UserControl
	{
		public SelectFromCatalog()
		{
			InitializeComponent();

			IsVisibleChanged += SelectFromCatalog_IsVisibleChanged;

			Catalogs.GotFocus += (sender, args) => {
				Catalogs.IsDropDownOpen = true;
			};

			Producers.GotFocus += (sender, args) => {
				Producers.IsDropDownOpen = true;
			};
		}

		private void SelectFromCatalog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue == false)
				return;
			Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate() { Catalogs.Focus(); }));
		}
	}
}
