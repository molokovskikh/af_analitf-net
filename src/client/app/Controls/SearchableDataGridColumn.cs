using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Common.Tools;

namespace AnalitF.Net.Client.Controls
{
	public class SearchableDataGridColumn : DataGridTextColumn
	{
		public static DependencyProperty SearchTermProperty
			= DependencyProperty.RegisterAttached("SearchTerm", typeof(string), typeof(SearchableDataGridColumn));

		public static DependencyProperty HighlightStyleProperty
			= DependencyProperty.RegisterAttached("HighlightStyle", typeof(Style), typeof(SearchableDataGridColumn));

		/// <summary>
		/// Графические элементы из которых строится таблица используются повторно
		/// когда мы манипулируем Inlines мы сбрасываем биндинг и текст элемента не изменится когда изменится DataContext
		/// по этому при изменении DataContext восстанавливаем биндинг и обновляем выделение
		/// </summary>
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var element = (TextBlock) base.GenerateElement(cell, dataItem);
			element.DataContextChanged += (s, a) => {
				var textBlock = ((TextBlock)s);
				BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, Binding);
				//очередная необъяснимая ошибка
				//если вызвать метод напрямую то для первого захода будет складываться впечатление что
				//он не дал результата хотя метод вызывается и раскраска происходит
				//подозреваю что биндинг применяется еще раз и сбрасывает всю работу сделанную в MarkText
				//по этому планируем работу после биндинга
				//то что описано выше теория, воспроизвести ситуацию изолировано мне не удалось
				Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() => MarkText(textBlock)));
			};
			return element;
		}

		private void MarkText(TextBlock element)
		{
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