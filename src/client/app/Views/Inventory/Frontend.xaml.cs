using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Views.Inventory
{
	public partial class Frontend : UserControl
	{
		public ViewModels.Inventory.Frontend Model => DataContext as ViewModels.Inventory.Frontend;

		public Frontend()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				Input.Focus();
			};

			DataContextChanged += (sender, args) => {
				if (Model == null)
					return;
				var handler = new BarcodeHandler(this, Model.Settings);
				handler.Barcode.Subscribe(x => Model.BarcodeScanned(x));
			};

			Keyboard.AddPreviewKeyDownHandler(this, (sender, args) => {
				IEnumerable<IResult> results = null;
				if (args.Key == Key.Multiply) {
					Model.InputQuantity();
					args.Handled = true;
				} else if (args.Key == Key.Q && ((Keyboard.Modifiers & ModifierKeys.Control) != 0)) {
					Model.UpdateQuantity();
					args.Handled = true;
				} else if (args.Key == Key.System && args.SystemKey == Key.Delete && ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)) {
					Model.Cancel();
					args.Handled = true;
				} else if (args.Key == Key.Escape) {
					Model.Quantity.Value = null;
				} else if (args.Key == Key.F1) {
					results = Model.Help();
				} else if (args.Key == Key.F2) {
					Model.SearchByProductId();
				} else if (args.Key == Key.F3) {
					Model.SearchByBarcode();
				} else if (args.Key == Key.F4) {
					Model.Trigger();
				} else if (args.Key == Key.F6) {
					results = Model.SearchByTerm();
				} else if (args.Key == Key.F7) {
					results = Model.SearchByCost();
				} else if (args.Key == Key.Enter) {
					results = Model.Close();
				}
				if (results != null) {
					Coroutine.BeginExecute(results.GetEnumerator(), new ActionExecutionContext { View = this });
					args.Handled = true;
				}
			});
		}
	}
}
