using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogOfferView : UserControl
	{
		public CatalogOfferView()
		{
			InitializeComponent();
			var grid = Offers;
			Loaded += (sender, args) => {
				var model = DataContext as CatalogOfferViewModel;
				if (model != null && model.IsFilterByCatalogName)
					Offers.Items.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));
			};

			EditBehavior.Attach(grid);
			ContextMenuBehavior.Attach(grid);

			Offers.TextInput += (sender, args) => {
				args.Handled = true;
				var model = DataContext as CatalogOfferViewModel;
				if (model != null)
					model.SearchInCatalog(args.Text);
			};

			CalculateColumnWidth(Offers, "00.00", "Наценка поставщика");
			CalculateColumnWidth(Offers, "000.00", "Цена производителя");
			CalculateColumnWidth(Offers, "000.00", "Пред.зарег.цена");
			CalculateColumnWidth(Offers, "0000.00", "Цена поставщика");
		}

		private void CalculateColumnWidth(DataGrid dataGrid, string template, string header)
		{
			var column = dataGrid.Columns.FirstOrDefault(c => c.Header.Equals(header));
			if (column == null)
				return;

			var text = new FormattedText(template,
				CultureInfo.CurrentUICulture,
				dataGrid.FlowDirection,
				new Typeface(dataGrid.FontFamily, dataGrid.FontStyle, dataGrid.FontWeight, dataGrid.FontStretch),
				dataGrid.FontSize,
				dataGrid.Foreground);
			column.Width = new DataGridLength(text.Width, DataGridLengthUnitType.Star);
		}
	}
}
