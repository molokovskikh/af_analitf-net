using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogView : UserControl
	{
		public CatalogView()
		{
			InitializeComponent();
			CatalogNames.Items.Clear();
			CatalogForms.Items.Clear();

			CatalogNames.TextInput += (sender, args) => {
				if (Char.IsControl(args.Text[0]))
					return;
				SearchText.Text += args.Text;
				DoSearch();
			};

			CatalogNames.KeyDown += (sender, args) => {
				if (args.Key == Key.Return)
					XamlExtentions.Focus(CatalogForms);
				if (args.Key == Key.Escape)
					SearchText.Text = "";
			};

			CatalogForms.KeyDown += (sender, args) => {
				if (args.Key == Key.Escape)
					XamlExtentions.Focus(CatalogNames);
			};

			Loaded += (sender, args) => {
				XamlExtentions.Focus(CatalogNames);
			};
		}

		private void DoSearch()
		{
			var text = SearchText.Text;
			var result = CatalogNames.Items.Cast<CatalogName>().FirstOrDefault(n => n.Name.ToLower().StartsWith(text));
			if (result != null) {
				CatalogNames.SelectedItem = result;
				XamlExtentions.Focus(CatalogNames);
			}
		}
	}
}
