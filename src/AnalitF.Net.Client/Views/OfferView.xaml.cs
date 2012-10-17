using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.Views
{
	public partial class OfferView : UserControl
	{
		public OfferView()
		{
			InitializeComponent();
			var grid = Offers;
			Loaded += (sender, args) => {
				XamlExtentions.Focus(grid);
			};

			EditBehavior.Attach(grid);
			ContextMenuBehavior.Attach(grid);
		}
	}
}
