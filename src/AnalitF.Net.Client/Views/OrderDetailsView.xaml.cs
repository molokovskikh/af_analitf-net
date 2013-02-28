using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class OrderDetailsView : UserControl
	{
		public OrderDetailsView()
		{
			InitializeComponent();

			ContextMenuBehavior.Attach(Lines);
			DataGridHelper.CalculateColumnWidths(Lines);
			EditBehavior.Attach(Lines);
		}
	}
}
