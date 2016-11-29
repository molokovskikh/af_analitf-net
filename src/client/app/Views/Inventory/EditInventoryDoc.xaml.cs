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
			DataContextChanged += OnDataContextChanged;

			Lines.BeginningEdit += (sender, args) =>
			{
				Lines.Tag = ((InventoryDocLine)args.Row.Item).Quantity;
			};

			Lines.RowEditEnding += (sender, args) => {
				var line = (InventoryDocLine)args.Row.Item;
				var oldQuantity = (decimal)Lines.Tag;
				model.UpdateQuantity(line, oldQuantity);
			};
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			model = DataContext as ViewModels.Inventory.EditInventoryDoc;
			if (model == null)
				return;
			DataContextChanged -= OnDataContextChanged;
		}
	}
}
