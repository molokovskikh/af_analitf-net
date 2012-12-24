using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;

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
			};

			//todo: если поставить фокус в строку поиска и ввести запрос
			//для товара который не отображен на экране
			//то выделение переместится к этому товару но прокрутка не будет произведена
			CatalogNames.KeyDown += (sender, args) => {
				var model = DataContext as CatalogViewModel;
				if (args.Key == Key.Return) {
					if (model == null || model.ViewOffersByCatalog)
						XamlExtentions.Focus(CatalogForms);
					else
						model.ShowAllOffers();
				}
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
	}
}
