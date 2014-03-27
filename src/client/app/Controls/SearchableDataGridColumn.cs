using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;

namespace AnalitF.Net.Client.Controls
{
	public class SearchableDataGridColumn : DataGridTextColumn
	{
		public static DependencyProperty SearchTermProperty
			= DependencyProperty.RegisterAttached("SearchTerm", typeof(string), typeof(SearchableDataGridColumn));

		public static DependencyProperty HighlightStyleProperty
			= DependencyProperty.RegisterAttached("HighlightStyle", typeof(Style), typeof(SearchableDataGridColumn));

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var element = (TextBlock) base.GenerateElement(cell, dataItem);
			element.Loaded += MarkText;
			return element;
		}

		private void MarkText(object sender, RoutedEventArgs e)
		{
			var element = ((TextBlock)sender);
			element.Loaded -= MarkText;
			var term = GetSearchTerm(DataGridOwner);
			if (String.IsNullOrEmpty(term))
				return;
			var text = element.Text;
			var index = text.IndexOf(term, StringComparison.CurrentCultureIgnoreCase);
			if (index < 0)
				return;

			element.Inlines.Clear();

			if (index > 0)
				element.Inlines.Add(new Run(text.Substring(0, index)));

			element.Inlines.Add(new Run(text.Substring(index, term.Length)) {
				Style = HighlightStyle
			});

			var left = text.Length - term.Length - index;
			if (left > 0)
				element.Inlines.Add(new Run(text.Substring(index + term.Length, left)));
		}

		public static string GetSearchTerm(DependencyObject d)
		{
			return (string)d.GetValue(SearchTermProperty);
		}

		public static void SetSearchTerm(DependencyObject d, string value)
		{
			d.SetValue(SearchTermProperty, value);
		}

		public Style HighlightStyle
		{
			get { return (Style)GetValue(HighlightStyleProperty); }
			set { SetValue(HighlightStyleProperty, value); }
		}
	}
}