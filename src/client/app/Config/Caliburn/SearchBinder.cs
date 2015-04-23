using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public class SearchBinder
	{
		public static DependencyProperty GridProperty
			= DependencyProperty.RegisterAttached("Grid", typeof(string), typeof(SearchBinder));

		public static void Bind(Type type, IEnumerable<FrameworkElement> elements, List<FrameworkElement> binded)
		{
			var searchMethod = type.GetProperty("SearchBehavior");
			if (searchMethod == null)
				return;

			var textBox = elements.FindName("SearchText") as TextBox;
			if (textBox == null)
				return;
			var gridName = GetGrid(textBox);
			if (String.IsNullOrEmpty(gridName))
				return;
			var grid = elements.FindName(gridName) as DataGrid;
			if (grid == null)
				return;

			grid.ObservableTextInput()
				.Where(e => {
					var model = GetModel(grid);
					return model != null && model.HandleGridKeyboardInput;
				})
				.CatchSubscribe(e => QuickSearchBehavior.RedirectInput(e, textBox));

			AttachKeyDown(grid);
			AttachKeyDown(textBox);
		}

		private static void AttachKeyDown(FrameworkElement element)
		{
			var observable = Observable.FromEventPattern<KeyEventArgs>(element, "KeyDown");
			observable
				.Where(a => a.EventArgs.Key == Key.Return)
				.CatchSubscribe(e => {
					var model = GetModel(element);
					if (model == null)
						return;
					ViewModelHelper.ProcessResult(model.Search(), new ActionExecutionContext {
						Source = e.Sender as FrameworkElement,
						EventArgs = e.EventArgs
					});
				});
			observable
				.Where(a => a.EventArgs.Key == Key.Escape)
				.CatchSubscribe(e => {
					var model = GetModel(element);
					if (model == null)
						return;
					ViewModelHelper.ProcessResult(model.ClearSearch(), new ActionExecutionContext {
						Source = e.Sender as FrameworkElement,
						EventArgs = e.EventArgs
					});
				});
		}

		private static SearchBehavior GetModel(FrameworkElement element)
		{
			var dc = element.DataContext;
			if (dc == null)
				return null;
			var propertyInfo = dc.GetType().GetProperty("SearchBehavior");
			if (propertyInfo == null)
				return null;
			var behavior = propertyInfo.GetValue(dc, null) as SearchBehavior;
			return behavior;
		}

		public static string GetGrid(DependencyObject d)
		{
			return (string)d.GetValue(GridProperty);
		}

		public static void SetGrid(DependencyObject d, string value)
		{
			d.SetValue(GridProperty, value);
		}
	}
}