using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;

namespace AnalitF.Net.Client.Binders
{
	public class EditBehavior
	{
		private static TimeSpan inputInterval = TimeSpan.FromMilliseconds(1500);
		private static TimeSpan commitInterval = TimeSpan.FromMilliseconds(750);

		public static void Attach(Controls.DataGrid grid)
		{
			var textInput = Observable
				.FromEventPattern<TextCompositionEventArgs>(grid, "TextInput")
				.Where(e => IsUint32(e.EventArgs.Text))
				.Do(e => e.EventArgs.Handled = true);

			textInput
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

			var backspace = keydown.Where(e => e.EventArgs.Key == Key.Back)
				.Do(e => e.EventArgs.Handled = true);

			backspace.Subscribe(e => UpdateValue(e.Sender, e, (v, ev) => v.Slice(0, -2)));

			keydown.Where(e => e.EventArgs.Key == Key.Delete)
				.Do(e => e.EventArgs.Handled = true)
				.Subscribe(e => UpdateValue(e.Sender, e, (v, ev) => ""));

			var updated = backspace.Select(e => e.Sender).Merge(textInput.Select(e => e.Sender));
			//игнорировать события до тех пор пока не произошло событие редактирования
			//когда произошло взять одно событие и повторить, фактически это state machine
			//которая генерирует событие OfferCommitted только если было событие редактирования
			updated.Throttle(commitInterval)
				.Merge(Observable.FromEventPattern<EventArgs>(grid, "CurrentCellChanged").Select(e => e.Sender))
				.Merge(Observable.FromEventPattern<RoutedEventArgs>(grid, "Unloaded").Select(e => e.Sender))
				.SkipUntil(updated)
				.Take(1)
				.Repeat()
				.ObserveOn(DispatcherScheduler.Current)
				.Subscribe(e => ViewModelHelper.InvokeDataContext(e, "OfferCommitted"));
		}

		private static bool IsUint32(string text)
		{
			uint v;
			return uint.TryParse(text, out v);
		}

		public static void UpdateValue<T>(object sender, T e, Func<string, T, string> value)
		{
			var dataGrid = (DataGrid)sender;
			var item = dataGrid.SelectedItem as Offer;
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