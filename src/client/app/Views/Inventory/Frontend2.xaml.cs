using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Frontend2 : UserControl
	{
		public ViewModels.Inventory.Frontend2 Model => DataContext as ViewModels.Inventory.Frontend2;

		public Frontend2()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				DataGridHelper.Focus(Lines);
			};
			KeyDown += (sender, args) => {
				if (args.Key == Key.F7) {
					Execute(Model.Close());
				}
				if (args.Key == Key.F3) {
					Model.Clear();
				}
			};

			DataContextChanged += (sender, args) => {
				if (Model == null)
					return;
				var handler = new BarcodeHandler(this, Model.Settings);
				handler.Barcode.Subscribe(x => Execute(Model.BarcodeScanned(x)));
			};

			new Editable().Attach(Lines);
			StyleHelper.ApplyStyles(typeof(CheckLine), Lines, Application.Current.Resources, Legend);
		}

		private void Execute(IEnumerable<IResult> enumerable)
		{
			Coroutine.BeginExecute(enumerable.GetEnumerator(), new ActionExecutionContext {View = this});
		}
	}
}
