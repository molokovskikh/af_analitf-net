using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogNameView : UserControl
	{
		public CatalogNameView()
		{
			InitializeComponent();

			SizeChanged += (sender, args) => {
				CatalogNamesColumn.MaxWidth = args.NewSize.Width / 2;
			};

			Catalogs.KeyDown += (sender, args) => {
				if (args.Key == Key.Escape) {
					DataGridHelper.Focus(CatalogNames);
					args.Handled = true;
				}
			};

			ApplyStyles();
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(CatalogName), CatalogNames, Application.Current.Resources);
			StyleHelper.ApplyStyles(typeof(Catalog), Catalogs, Application.Current.Resources);
		}
	}
}
