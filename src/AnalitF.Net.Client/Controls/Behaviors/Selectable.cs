using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Controls.Behaviors
{
	public class Selectable : Behavior<CheckBox>
	{
		protected override void OnAttached()
		{
			AssociatedObject.Checked += (sender, args) => Update();
			AssociatedObject.Unchecked += (sender, args) => Update();
		}

		private void Update()
		{
			var grid = AssociatedObject.VisualParents<DataGrid>().FirstOrDefault();
			var cell = AssociatedObject.Parents<DataGridColumnHeader>().FirstOrDefault();
			if (cell == null)
				return;
			var column = cell.Column as DataGridBoundColumn;
			if (column == null)
				return;

			var binding = (Binding)column.Binding;
			foreach (var item in grid.Items) {
				Util.SetValue(item, binding.Path.Path, AssociatedObject.IsChecked.GetValueOrDefault());
			}
		}
	}
}