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

			SearchBehavior.AttachSearch(CatalogNames, SearchText);

			CatalogNames.TextInput += (sender, args) => {
				if (Char.IsControl(args.Text[0]))
					return;
				SearchText.Text += args.Text;
				DataGridHelper.Centrify(CatalogNames);
			};

			//todo: если поставить фокус в строку поиска и ввести запрос
			//для товара который не отображен на экране
			//то выделение переместится к этому товару но прокрутка не будет произведена
			CatalogNames.KeyDown += (sender, args) => {
				var model = DataContext as CatalogViewModel;
				if (args.Key == Key.Return) {
					if (model == null || model.ViewOffersByCatalog)
						DataGridHelper.Focus(CatalogForms);
					else
						model.ShowAllOffers();
				}
			};

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
