using System;
using System.Collections.Generic;
using System.Linq;
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

		public static DependencyProperty SplitTerm
			= DependencyProperty.RegisterAttached("SplitTerm", typeof(bool), typeof(SearchableDataGridColumn),
				new PropertyMetadata(false));
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
			if (String.IsNullOrEmpty(term))
				return;

			if (GetSplitTerm(DataGridOwner)) {
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
			else {
				var parts = Split(text, term.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
				if (parts.Length > 0)
					element.Inlines.Clear();
				foreach (var tuple in parts) {
					var run = new Run(text.Substring(tuple.Item1, tuple.Item2));
					if (tuple.Item3)
						run.Style = HighlightStyle;
					element.Inlines.Add(run);
				}
			}
		}

		public static IEnumerable<Tuple<int, int, bool>> Split(string src, string[] terms)
		{
			var list = new List<Tuple<int, int, bool>>();
			//формируем список совпадений объединяя пересекающиеся
			foreach (var term in terms) {
				var start = 0;
				int foundStart;
				while ((foundStart = src.IndexOf(term, start, StringComparison.CurrentCultureIgnoreCase)) >= 0) {
					var foundEnd = foundStart + term.Length;
					var exists = list.FirstOrDefault(l => (foundStart >= l.Item1 && foundStart <= l.Item1 + l.Item2)
						|| (foundEnd >= l.Item1 && foundEnd <= l.Item1 + l.Item2));
					if (exists != null) {
						var newBegin = Math.Min(exists.Item1, foundStart);
						var newEnd = Math.Max(exists.Item1 + exists.Item2, foundEnd);
						var newLen = newEnd - newBegin;
						if (newBegin != exists.Item1 || newLen != exists.Item2) {
							list.Remove(exists);
							list.Add(Tuple.Create(newBegin, newLen, true));
						}
					}
					else {
						list.Add(Tuple.Create(foundStart, term.Length, true));
					}
					start = foundEnd;
					if (start >= term.Length)
						break;
				}
			}
			if (list.Count == 0)
				return Enumerable.Empty<Tuple<int, int, bool>>();
			//дополняем список не совпавшими позициями
			var result = new List<Tuple<int, int, bool>>();
			var consumed = 0;
			foreach (var item in list.OrderBy(l => l.Item1)) {
				if (consumed < item.Item1) {
					var length = item.Item1 - consumed;
					result.Add(Tuple.Create(consumed, length, false));
					consumed += length;
				}
				result.Add(item);
				consumed += item.Item2;
			}
			if (consumed < src.Length)
				result.Add(Tuple.Create(consumed, src.Length - consumed, false));
			return result;
		}

		public static string GetSearchTerm(DependencyObject d)
		{
			return (string)d.GetValue(SearchTermProperty);
		}

		public static void SetSearchTerm(DependencyObject d, string value)
		{
			d.SetValue(SearchTermProperty, value);
		}

		public static bool GetSplitTerm(DependencyObject d)
		{
			return (bool)d.GetValue(SplitTerm);
		}

		public static void SetSplitTerm(DependencyObject d, bool value)
		{
			d.SetValue(SplitTerm, value);
		}

		public Style HighlightStyle
		{
			get { return (Style)GetValue(HighlightStyleProperty); }
			set { SetValue(HighlightStyleProperty, value); }
		}
	}
}