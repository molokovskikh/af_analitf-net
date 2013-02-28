using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views
{
	public partial class OrderLinesView : UserControl
	{
		public OrderLinesView()
		{
			InitializeComponent();

			ContextMenuBehavior.Attach(Lines);
			ContextMenuBehavior.Attach(SentLines);
			DataGridHelper.CalculateColumnWidths(Lines);
			DataGridHelper.CalculateColumnWidths(SentLines);
			EditBehavior.Attach(Lines);
		}
	}
}
