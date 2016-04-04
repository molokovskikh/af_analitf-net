using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Dialogs;

namespace AnalitF.Net.Client.Views.Dialogs
{
	public class DropMarkAdorner : Adorner
	{
		private AdornerLayer layer;
		private FrameworkElement block;
		private Point begin;
		private Point end;
		private FrameworkElement baseEl;

		public DropMarkAdorner(FrameworkElement el)
			: base(el)
		{
			this.layer = AdornerLayer.GetAdornerLayer(el);
			this.baseEl = el;
			layer.Add(this);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (block == null)
				return;
			var pen = new Pen(Brushes.LightSeaGreen, 3);
			pen.DashCap = PenLineCap.Round;
			pen.DashStyle = DashStyles.DashDot;
			drawingContext.DrawLine(pen, begin, end);
		}

		public void Refresh(FrameworkElement el, bool vertical = false)
		{
			if (el == block)
				return;

			block = el;
			if (block != null) {
				if (vertical) {
					begin = el.TranslatePoint(new Point(el.ActualWidth, 0), baseEl);
					end = el.TranslatePoint(new Point(el.ActualWidth, el.ActualHeight), baseEl);
				} else {
					begin = el.TranslatePoint(new Point(0, 0), baseEl);
					end = el.TranslatePoint(new Point(el.ActualWidth, 0), baseEl);
				}
			}
			InvalidateVisual();
		}
	}

	public partial class PriceTagConstructorView : UserControl
	{
		private DropMarkAdorner dropAdorner;

		public PriceTagConstructorView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				dropAdorner = new DropMarkAdorner(Canvas);
			};

			Items.MouseMove += (sender, args) => {
				if (Items.SelectedItem != null && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed) {
					var el = CanvasPanel.ToBlock(new PriceTagItem((PriceTagItem)Items.SelectedItem));
					DragDrop.DoDragDrop(Items, new DataObject(el), DragDropEffects.All);
				}
			};
			Canvas.MouseMove += (sender, args) => {
				if (Canvas.Selected != null && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed) {
					DragDrop.DoDragDrop(Canvas, new DataObject(typeof(FrameworkElement), Canvas.Selected), DragDropEffects.All);
				}
			};
			Canvas.MouseDown += (sender, args) => {
				Canvas.Focus();
				Canvas.Selected = FindItem(args);
			};
			Canvas.DragEnter += (sender, args) => {
				args.Effects = DragDropEffects.None;
				var el = FindItem(args);
				var dragEl = (FrameworkElement)args.Data.GetData(typeof(FrameworkElement));
				dropAdorner.Refresh(el, vertical: !CanvasPanel.GetIsNewLine(dragEl));
			};
			Canvas.DragLeave += (sender, args) => {
				dropAdorner.Refresh(null);
			};
			Canvas.Drop += (sender, args) => {
				if (args.Data.GetDataPresent(typeof(FrameworkElement))) {
					var el = (FrameworkElement)args.Data.GetData(typeof(FrameworkElement));
					DropBlock(args, el);
				}
			};
		}

		private void DropBlock(DragEventArgs args, FrameworkElement el)
		{
			var position = Canvas.Children.IndexOf(FindItem(args));
			if (position >= 0) {
				Canvas.RemoveChild(el);
				Canvas.InsertChild(position, el);
			} else {
				Canvas.AddChild(el);
			}
			dropAdorner.Refresh(null);
		}

		private static FrameworkElement FindItem(RoutedEventArgs args)
		{
			var el = args.OriginalSource as FrameworkElement;
			return el.Parents().OfType<Border>().FirstOrDefault(x => x.DataContext is PriceTagItem);
		}
	}
}
