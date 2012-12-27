using System;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogView : UserControl
	{
		public CatalogView()
		{
			InitializeComponent();

			SearchBehavior.AttachSearch(CatalogNames, CatalogNamesSearch_SearchText);
			SearchBehavior.AttachSearch(CatalogForms, CatalogsSearch_SearchText);

			SizeChanged += (sender, args) => {
				CatalogNamesColumn.MaxWidth = args.NewSize.Width / 2;
			};

			CatalogForms.KeyDown += (sender, args) => {
				if (args.Key == Key.Escape) {
					DataGridHelper.Focus(CatalogNames);
					args.Handled = true;
				}
			};

			Loaded += (sender, args) => DataGridHelper.Focus(CatalogNames);
		}
	}
}
