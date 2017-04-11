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
using AnalitF.Net.Client.Controls.Behaviors;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for StockChooser.xaml
	/// </summary>
	public partial class StockChooser : UserControl
	{
		public ViewModels.Inventory.StockChooser Model => DataContext as ViewModels.Inventory.StockChooser;

		public StockChooser()
		{
			InitializeComponent();
			new Editable().Attach(Items);
			KeyDown += (sender, args) =>
			{
				if (args.Key == Key.D && ((Keyboard.Modifiers & ModifierKeys.Control) != 0))
				{
					Model.ShowDescription();
				}
			};
		}
	}
}
