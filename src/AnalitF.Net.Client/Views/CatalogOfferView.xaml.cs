using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.Views
{
	public partial class CatalogOfferView : UserControl
	{
		public CatalogOfferView()
		{
			InitializeComponent();
			var grid = Offers;
			Loaded += (sender, args) => {
				XamlExtentions.Focus(grid);
			};

			EditBehavior.Attach(grid);
			ContextMenuBehavior.Attach(grid);
		}

		private void InvokeViewModel(object sender, ExecutedRoutedEventArgs e)
		{
			ViewModelHelper.InvokeDataContext(sender, e.Parameter as string);
		}

		private void CanInvokeViewModel(object sender, CanExecuteRoutedEventArgs e)
		{
			var result = ViewModelHelper.InvokeDataContext(sender, "Can" + e.Parameter)
				?? ViewModelHelper.InvokeDataContext(sender, "get_Can" + e.Parameter);
			if (result is bool)
				e.CanExecute = (bool)result;
			else {
				e.CanExecute = true;
			}
		}
	}
}
