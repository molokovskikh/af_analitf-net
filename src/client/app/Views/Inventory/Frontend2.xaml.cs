using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
				Input.Focus();
			};

			DataContextChanged += (sender, args) => {
				if (Model == null)
					return;
				var handler = new BarcodeHandler(this, Model.Settings);
				handler.Barcode.Subscribe(x => Execute(Model.BarcodeScanned(x)));
			};

			Input.KeyDown += (sender, args) => {
				if (args.Key == Key.Enter) {
					Execute(Model.Enter());
				}
			};

			Lines.KeyDown += (sender, args) => {
				if (args.Key == Key.Delete)
					Model.Lines.RemoveAll(Lines.SelectedItems.Cast<CheckLine>());
			};
			new Editable().Attach(Lines);
		}

		private void Execute(IEnumerable<IResult> enumerable)
		{
			Coroutine.BeginExecute(enumerable.GetEnumerator(), new ActionExecutionContext {View = this});
		}
	}
}
