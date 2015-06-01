using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;

namespace AnalitF.Net.Client.Controls.Behaviors
{
	public interface IInlineEditable
	{
		uint Value { get; set; }
	}

	public class Editable
	{
		private static TimeSpan inputInterval = TimeSpan.FromMilliseconds(1500);
		private static TimeSpan commitInterval = TimeSpan.FromMilliseconds(750);
		private IScheduler scheduler;

		//для регистрации editor в xaml
		public static DependencyProperty EditorProperty = DependencyProperty.Register("Editor", typeof(Editor), typeof(Editable));

		public static void SetEditor(UIElement el, Editor value)
		{
			el.SetValue(EditorProperty, value);
		}

		public static Editor GetEditor(UIElement el)
		{
			return (Editor)el.GetValue(EditorProperty);
		}

		public Editable(IScheduler scheduler = null)
		{
			this.scheduler = scheduler ?? DispatcherScheduler.Current;
		}

		public void Attach(DataGrid grid)
		{
			var keydown = Observable.FromEventPattern<KeyEventArgs>(grid, "KeyDown");
			var textInput = Observable.FromEventPattern<TextCompositionEventArgs>(grid, "TextInput");

			var lastEdit = DateTime.MinValue;
			var edit = textInput
				.Where(e => NullableConvert.ToUInt32(e.EventArgs.Text) != null)
				.Do(e => e.EventArgs.Handled = true)
				.Select(e => new Func<string, string>(v => {
					var text = e.EventArgs.Text;
					var now = DateTime.Now;
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
				lastEdit = DateTime.Now;
			});

			//игнорировать события до тех пор пока не произошло событие редактирования
			//когда произошло взять одно событие и повторить, фактически это state machine
			//которая генерирует событие OfferCommitted только если было событие редактирования
			updated.Throttle(commitInterval, scheduler)
				.Merge(Observable.FromEventPattern<EventArgs>(grid, "CurrentCellChanged").Select(e => e.Sender))
				.Merge(Observable.FromEventPattern<RoutedEventArgs>(grid, "Unloaded").Select(e => e.Sender))
				.SkipUntil(updated)
				.Take(1)
				.Repeat()
				.ObserveOn(scheduler)
				.Subscribe(e => {
					lastEdit = DateTime.MinValue;
					var editor = GetEditor(grid);
					if (editor != null) {
						editor.Committed();
					}
					else {
						ViewModelHelper.InvokeDataContext(grid, "OfferCommitted");
					}
				});
		}

		private void UpdateValue(object sender, Func<string, string> value)
		{
			var dataGrid = (DataGrid)sender;
			var item = dataGrid.SelectedItem as IInlineEditable;
			if (item == null)
				return;
			item.Value = SafeConvert.ToUInt32(value(item.Value == 0 ? "" : item.Value.ToString()));
			var editor = GetEditor(dataGrid);
			if (editor != null) {
				editor.Updated();
			}
			else {
				ViewModelHelper.InvokeDataContext(dataGrid, "OfferUpdated");
			}
		}

		public static void AutoEditOnDigit(DataGrid2 grid, string name)
		{
			var column = DataGridHelper.FindColumn(grid.Columns, name);
			grid.TextInput += (sender, args) => {
				if (grid.SelectedItem == null)
					return;
				var isDigit = args.Text.All(Char.IsDigit);
				if (!isDigit)
					return;
				args.Handled = true;
				grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, column);
				grid.BeginEdit(args);
			};
		}
	}
}