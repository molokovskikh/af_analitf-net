using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class HistoryOrdersView : UserControl
	{
		public HistoryOrdersView()
		{
			InitializeComponent();
			StyleHelper.ApplyStyles(typeof(SentOrderLine), Lines, Application.Current.Resources);
		}
	}
}
