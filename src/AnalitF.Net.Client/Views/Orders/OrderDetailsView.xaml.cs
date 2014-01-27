using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class OrderDetailsView : UserControl
	{
		public OrderDetailsView()
		{
			InitializeComponent();

			DataGridHelper.CalculateColumnWidths(Lines);
			new Editable().Attach(Lines);
			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend);
		}
	}
}
