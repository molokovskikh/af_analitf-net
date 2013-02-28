using System.Windows.Controls;
using AnalitF.Net.Client.Binders;

namespace AnalitF.Net.Client.Views
{
	public partial class OrdersView : UserControl
	{
		public OrdersView()
		{
			InitializeComponent();

			ContextMenuBehavior.Attach(Orders);
		}
	}
}
