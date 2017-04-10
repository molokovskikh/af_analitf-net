using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for CatalogChooser.xaml
	/// </summary>
	public partial class CatalogChooser : UserControl
	{
		public ViewModels.Inventory.CatalogChooser Model => DataContext as ViewModels.Inventory.CatalogChooser;

		public CatalogChooser()
		{
			InitializeComponent();
			KeyDown += (sender, args) =>
			{
				if (args.Key == Key.F1)
				{
					Model.ShowDescription();
				}
			};

		}
	}
}
