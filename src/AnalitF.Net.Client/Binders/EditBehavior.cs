using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;

namespace AnalitF.Net.Client.Binders
{
	public class EditBehavior
	{
		public static void Attach(Controls.DataGrid grid)
		{
			var inputInterval = TimeSpan.FromMilliseconds(1500);
			Observable.FromEventPattern<TextCompositionEventArgs>(grid, "TextInput")
				.Where(e => {
					int v;
					return Int32.TryParse(e.EventArgs.Text, out v);
				})
				.Do(e => e.EventArgs.Handled = true)
				.TimeInterval()
				.Subscribe(e => UpdateValue(e.Value.Sender, e, (value, ev) => {
					if (ev.Interval > inputInterval)
						value = ev.Value.EventArgs.Text;
					else {
						value = value + ev.Value.EventArgs.Text;
					}
					return value;
				}));

			var keydown = Observable.FromEventPattern<KeyEventArgs>(grid, "KeyDown");

			keydown.Where(e => e.EventArgs.Key == Key.Back)
				.Do(e => e.EventArgs.Handled = true)
				.Subscribe(e => UpdateValue(e.Sender, e, (v, ev) => v.Slice(0, -2)));

			keydown.Where(e => e.EventArgs.Key == Key.Delete)
				.Do(e => e.EventArgs.Handled = true)
				.Subscribe(e => UpdateValue(e.Sender, e, (v, ev) => ""));
		}

		public static void UpdateValue<T>(object sender, T e, Func<string, T, string> value)
		{
			var dataGrid = (DataGrid)sender;
			var item = dataGrid.CurrentItem as Offer;
			if (item == null)
				return;
			item.OrderCount = SafeConvert.ToUInt32(value(item.OrderCount.ToString(), e));
			var viewModel = dataGrid.DataContext as BaseOfferViewModel;
			if (viewModel != null) {
				viewModel.OfferUpdated();
			}
		}
	}
}