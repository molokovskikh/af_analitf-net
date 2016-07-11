using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Common.Tools;
using NHibernate.Util;

namespace AnalitF.Net.Client.Views.Dialogs
{
	public class SelectAdorner : Adorner
	{
		private FrameworkElement el;

		public SelectAdorner(UIElement adornedElement) : base(adornedElement)
		{
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (el == null)
				return;

			var point = el.TranslatePoint(new Point(0, 0), AdornedElement);
			drawingContext.DrawRectangle(Brushes.Transparent, new Pen(Brushes.LightBlue, 2),
				new Rect(point, new Size(el.ActualWidth, el.ActualHeight)));
		}

		public void Select(FrameworkElement el)
		{
			this.el = el;
			InvalidateVisual();
		}
	}

	public class StaticCanvasPanel : Panel
	{
		public static DependencyProperty IsNewLineProperty
			= DependencyProperty.RegisterAttached("IsNewLine", typeof(bool), typeof(CanvasPanel), new FrameworkPropertyMetadata(IsNewLineChanged));

		private static void IsNewLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var parent = ((FrameworkElement)d).Parent;
			if (parent == null)
				return;
			((CanvasPanel)parent).InvalidateArrange();
		}

		public static bool GetIsNewLine(DependencyObject o)
		{
			return (bool)o.GetValue(IsNewLineProperty);
		}

		public static void SetIsNewLine(DependencyObject o, bool value)
		{
			o.SetValue(IsNewLineProperty, value);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			double width = 0;
			double height = 0;
			double lineWidth = 0;
			double lineHeight = 0;
			foreach (UIElement child in Children) {
				child.Measure(availableSize);
				if (GetIsNewLine(child)) {
					width = Math.Max(width, lineWidth);
					height += lineHeight;
					lineHeight = 0;
					lineWidth = 0;
				}
				lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
				lineWidth += child.DesiredSize.Width;
			}
			return availableSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double width = 0;
			double height = 0;
			double lineWidth = 0;
			double lineHeight = 0;
			var cellWidth = finalSize.Width;
			for(var i = 0; i < Children.Count; i++) {
				var child = Children[i];
				if (height >= finalSize.Height)
					break;
				if (GetIsNewLine(child) || i == 0) {
					width = Math.Max(finalSize.Width - width, lineWidth);
					height += lineHeight;
					lineHeight = 0;
					lineWidth = 0;
					cellWidth = finalSize.Width;
					var cellInLine = 1;
					for(var j = i + 1; j < Children.Count; j++) {
						if (GetIsNewLine(Children[j]))
							break;
						cellInLine++;
					}
					cellWidth = cellWidth / cellInLine;
				}
				var cellHeight = Math.Min(finalSize.Height - height, child.DesiredSize.Height);

				child.Arrange(new Rect(lineWidth, height, cellWidth, cellHeight));
				child.RenderSize = new Size(cellWidth, cellHeight);

				lineHeight = Math.Max(lineHeight, cellHeight);
				lineWidth += cellWidth;
			}
			return finalSize;
		}
	}

	public class CanvasPanel : StaticCanvasPanel
	{
		public static DependencyProperty SelectedProperty
			= DependencyProperty.Register("Selected", typeof(FrameworkElement), typeof(CanvasPanel),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedChanged));

		public static DependencyProperty ItemsProperty
			= DependencyProperty.Register("Items", typeof(ObservableCollection<PriceTagItem>), typeof(CanvasPanel),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ItemsChanged));

		private static void ItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((CanvasPanel)d).ItemsChangedInstance(d, e);
		}

		private void ItemsChangedInstance(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue != null) {
				((ObservableCollection<PriceTagItem>)e.NewValue).CollectionChanged += CollectionChanged;
				Children.Clear();
				foreach (var item in Items)
					Children.Add(ToBlock(item));
			}
			if (e.OldValue != null)
				((ObservableCollection<PriceTagItem>)e.OldValue).CollectionChanged -= CollectionChanged;
		}

		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Remove) {
				foreach (var item in e.OldItems)
				{
					var el = Children.OfType<FrameworkElement>().FirstOrDefault(x => x.DataContext == item);
					if (el != null)
						Children.Remove(el);
				}
			} else if (e.Action == NotifyCollectionChangedAction.Add) {
				foreach (var item in e.NewItems.Cast<PriceTagItem>()) {
					var el = Children.OfType<FrameworkElement>().FirstOrDefault(x => x.DataContext == item);
					if (el == null)
						Children.Add(ToBlock(item));
				}
			} else if (e.Action == NotifyCollectionChangedAction.Reset) {
				Children.Clear();
			}
		}

		private static void SelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((CanvasPanel)d).selectAdorner.Select((FrameworkElement)e.NewValue);
		}

		private SelectAdorner selectAdorner;

		public CanvasPanel()
		{
			Focusable = true;
			AllowDrop = true;
			Background = Brushes.LightGray;

			Loaded += (sender, args) => {
				var adornerLayer = AdornerLayer.GetAdornerLayer(this);
				selectAdorner = new SelectAdorner(this);
				adornerLayer.Add(selectAdorner);
			};
		}

		public ObservableCollection<PriceTagItem> Items
		{
			get { return (ObservableCollection<PriceTagItem>)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		public FrameworkElement Selected
		{
			get { return (FrameworkElement)GetValue(SelectedProperty); }
			set { SetValue(SelectedProperty, value); }
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.Key == Key.Up) {
				if (Selected == null) {
					Selected = Children.OfType<FrameworkElement>().LastOrDefault();
				} else {
					var items = Children.OfType<FrameworkElement>().ToArray();
					var index = items.IndexOf(Selected) - 1;
					if (index >= 0) {
						Selected = items[index];
					}
				}
				e.Handled = true;
			}
			if (e.Key == Key.Down) {
				if (Selected == null) {
					Selected = Children.OfType<FrameworkElement>().FirstOrDefault();
				} else {
					var items = Children.OfType<FrameworkElement>().ToArray();
					var index = items.IndexOf(Selected) + 1;
					if (index < items.Length) {
						Selected = items[index];
					}
				}
				e.Handled = true;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Dispatcher.BeginInvoke(new Action(() => selectAdorner.InvalidateVisual()), DispatcherPriority.ApplicationIdle);

			return base.ArrangeOverride(finalSize);
		}

		public void Clear()
		{
			Children.Clear();
			Items?.Clear();
			Selected = null;
		}

		public void AddChild(FrameworkElement el)
		{
			if (Children.OfType<FrameworkElement>().Contains(el)) {
				Children.Remove(el);
				Items?.Remove((PriceTagItem)el.DataContext);
			}
			Children.Add(el);
			Items?.Add((PriceTagItem)el.DataContext);
		}

		public void InsertChild(int position, FrameworkElement el)
		{
			Children.Insert(position, el);
			Items?.Insert(position, (PriceTagItem)el.DataContext);
		}

		public void RemoveChild(FrameworkElement el)
		{
			Children.Remove(el);
			Items?.Remove((PriceTagItem)el.DataContext);
		}

		public static FrameworkElement ToBlock(object value)
		{
			var text = new TextBlock {
				Text = value.ToString(),
				DataContext = value
			};
			BindingOperations.SetBinding(text, TextBlock.TextProperty, new Binding("DisplayText"));
			var border = new Border {
				Child = text,
				DataContext = value
			};
			BindingOperations.SetBinding(border, CanvasPanel.IsNewLineProperty, new Binding("IsNewLine"));
			return border;
		}
	}
}