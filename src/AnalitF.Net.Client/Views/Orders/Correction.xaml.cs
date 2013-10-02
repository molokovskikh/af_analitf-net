using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class Correction : UserControl
	{
		public Correction()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				//Lines.Items.GroupDescriptions.Add(new PropertyGroupDescription("Order.Address"));
				Lines.Items.GroupDescriptions.Add(new PropertyGroupDescription("Order"));
			};

			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, context: "Correction");
		}
	}
}
