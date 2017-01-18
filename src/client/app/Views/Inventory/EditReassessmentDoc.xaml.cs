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
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using Common.Tools;

namespace AnalitF.Net.Client.Views.Inventory
{
	public class Command : ICommand
	{
		private Action action;
		private bool canExecute;

		public Command(Action action, IObservable<bool> canExecute)
		{
			this.action = action;
			canExecute.Subscribe(x => {
				this.canExecute = x;
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			});
		}

		public void Execute(object parameter)
		{
			action();
		}

		public bool CanExecute(object parameter)
		{
			return canExecute;
		}

		public event EventHandler CanExecuteChanged;
	}

	public partial class EditReassessmentDoc : UserControl
	{
		public EditReassessmentDoc()
		{
			InitializeComponent();
			DataContextChanged += d;

			Lines.Columns.Insert(0, DataGridHelper.CheckBoxColumn("", "Selected",
				x => Lines.Items.OfType<ReassessmentLine>().Each(y => y.Selected = x),
				false));
		}

		private void d(object sender, DependencyPropertyChangedEventArgs e)
		{
			var model = DataContext as ViewModels.Inventory.EditReassessmentDoc;
			if (model == null)
				return;

			Lines.KeyDown += (o, args) => {
				if (args.Key == Key.Delete) {
					model.Delete();
				}
			};
		}
	}
}
