using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;

namespace AnalitF.Net.Client.Binders
{
	public interface IInlineEditable
	{
		uint Value { get; set; }
	}

	public class EditBehavior
	{
		private static TimeSpan inputInterval = TimeSpan.FromMilliseconds(1500);
		private static TimeSpan commitInterval = TimeSpan.FromMilliseconds(750);
		public static DispatcherScheduler UIScheduler = DispatcherScheduler.Current;

		public static void Attach(Controls.DataGrid grid)
		{
			var keydown = Observable.FromEventPattern<KeyEventArgs>(grid, "KeyDown");
			var textInput = Observable.FromEventPattern<TextCompositionEventArgs>(grid, "TextInput");

			var lastEdit = TimeSpan.Zero;
			var edit = textInput
				.Where(e => NullableConvert.ToUInt32(e.EventArgs.Text) != null)
				.Do(e => e.EventArgs.Handled = true)
				.Select(e => new Func<string, string>(v => {
					var text = e.EventArgs.Text;
					var now = TimeSpan.FromMilliseconds(Environment.TickCount);
					if ((now - lastEdit) > inputInterval)
						return text;
					else {
						return v + text;
					}
				}));

			var backspace = keydown.Where(e => e.EventArgs.Key == Key.Back)
				.Do(e => e.EventArgs.Handled = true)
				.Select(e => new Func<string, string>(v => v.Slice(0, -2)));

			var delete = keydown.Where(e => e.EventArgs.Key == Key.Delete)
				.Do(e => e.EventArgs.Handled = true)
				.Select(e => new Func<string, string>(v => ""));

			var updated = edit.Merge(backspace).Merge(delete);

			updated.Subscribe(a => {
				UpdateValue(grid, a);
				lastEdit = TimeSpan.FromMilliseconds(Environment.TickCount);
			});

			//игнорировать события до тех пор пока не произошло событие редактирования
			//когда произошло взять одно событие и повторить, фактически это state machine
			//которая генерирует событие OfferCommitted только если было событие редактирования
			updated.Throttle(commitInterval)
				.Merge(Observable.FromEventPattern<EventArgs>(grid, "CurrentCellChanged").Select(e => e.Sender))
				.Merge(Observable.FromEventPattern<RoutedEventArgs>(grid, "Unloaded").Select(e => e.Sender))
				.SkipUntil(updated)
				.Take(1)
				.Repeat()
				.ObserveOn(UIScheduler)
				.Subscribe(e => {
					lastEdit = TimeSpan.Zero;
					ViewModelHelper.InvokeDataContext(e, "OfferCommitted");
				});
		}

		public static void UpdateValue(object sender, Func<string, string> value)
		{
			var dataGrid = (DataGrid)sender;
			var item = dataGrid.SelectedItem as IInlineEditable;
			if (item == null)
				return;
			item.Value = SafeConvert.ToUInt32(value(item.Value == 0 ? "" : item.Value.ToString()));
			var viewModel = dataGrid.DataContext;
			if (viewModel != null)
				ViewModelHelper.InvokeDataContext(dataGrid, "OfferUpdated");
		}
	}
}