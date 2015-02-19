using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogSearchView : UserControl
	{
		public CatalogSearchView()
		{
			InitializeComponent();
			SearchText.KeyDown += (sender, args) => {
				if (args.Key == Key.Return) {
					DataGridHelper.Focus(Items);
				}
			};
			ApplyStyles();
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(CatalogDisplayItem), Items, Application.Current.Resources);
		}
	}
}
