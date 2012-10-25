using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class PriceOfferView : UserControl
	{
		public PriceOfferView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				XamlExtentions.Focus(Offers);
			};

			EditBehavior.Attach(Offers);
			ContextMenuBehavior.Attach(Offers);
		}
	}
}
