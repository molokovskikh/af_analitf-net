using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class OrderLinesView : UserControl
	{
		public OrderLinesView()
		{
			InitializeComponent();

			DataGridHelper.CalculateColumnWidths(Lines);
			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend);
			DataGridHelper.CalculateColumnWidths(SentLines);
			new Editable().Attach(Lines);
		}
	}
}
