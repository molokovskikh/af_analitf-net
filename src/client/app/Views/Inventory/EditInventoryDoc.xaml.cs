using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Models.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
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
using Diadoc.Api.Proto.Documents;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for EditInventory.xaml
	/// </summary>
	public partial class EditInventoryDoc : UserControl
	{
		private ViewModels.Inventory.EditInventoryDoc model;

		public EditInventoryDoc()
		{
			InitializeComponent();
		}
	}
}
