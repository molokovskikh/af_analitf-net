using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Linq.Expressions;
using Xceed.Wpf.Toolkit.Core.Converters;

namespace AnalitF.Net.Client.Controls
{

	public class ScrollBarVisibilityChangedEventArgs : EventArgs
	{
		public enum ScrollBarTypes { Verical, Horizontal}

		public readonly ScrollBarVisibility OldValue;
		public readonly ScrollBarVisibility NewValue;
		public readonly ScrollBarTypes ScrollBarType;

		public ScrollBarVisibilityChangedEventArgs(ScrollBarTypes scrollBarType, ScrollBarVisibility oldValue, ScrollBarVisibility newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
			ScrollBarType = scrollBarType;
		}
	}

	public class ScrollMainContentViewer: ScrollViewer
	{
		protected readonly Window BaseWindow;
		protected readonly double ScrollActivateHeigth = 600;
		public new ScrollBarVisibility VerticalScrollBarVisibility {
			get
			{
				return base.VerticalScrollBarVisibility;
			}
			set
			{
				ScrollBarVisibilityChanged(new ScrollBarVisibilityChangedEventArgs(
					ScrollBarVisibilityChangedEventArgs.ScrollBarTypes.Verical,  VerticalScrollBarVisibility, value
					));

				base.VerticalScrollBarVisibility = value;
			}
		}

		protected delegate void ScrollBarVisibilityEventHandler(ScrollBarVisibilityChangedEventArgs e);
		protected event ScrollBarVisibilityEventHandler ScrollBarVisibilityChanged;

		public ScrollMainContentViewer()
		{
			ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;
			BaseWindow = Application.Current.MainWindow;
			BaseWindow.SizeChanged += OnSizeChanged;
			GotFocus += OnGetFocus;
		}

		private void OnGetFocus(object sender, EventArgs e)
		{
			if (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
			{
				OptimizateGridsForVScrolling(true);
			}
			else
			{
				OptimizateGridsForVScrolling(false);
			}
		}

		protected virtual void OnScrollBarVisibilityChanged(ScrollBarVisibilityChangedEventArgs e)
		{
			if (e.ScrollBarType != ScrollBarVisibilityChangedEventArgs.ScrollBarTypes.Verical ||
				e.NewValue == e.OldValue) {
				return;
			}

			if (e.NewValue == ScrollBarVisibility.Disabled) {
				OptimizateGridsForVScrolling(true);
			} else {
				OptimizateGridsForVScrolling(false);
			}
		}

		protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			VerticalScrollBarVisibility = (BaseWindow.ActualHeight <= ScrollActivateHeigth)
				? ScrollBarVisibility.Auto
				: ScrollBarVisibility.Disabled;
		}

		private void OptimizateGridsForVScrolling(bool hideScrollBar)
		{
			Type grid2MainType = typeof(DataGrid2Main);
			Type grid2Type = typeof(DataGrid2);
			Type gridType = typeof(DataGrid);

			var controls = FindControlsByType(new Type[] {grid2MainType, grid2Type, gridType});

			if (!hideScrollBar) {
				ScrollToVerticalOffset(0);
			}

			foreach (Control control in controls) {

				if (control == null) continue;
				if (control.GetType() == grid2MainType) {
					if (!hideScrollBar) {
						control.Height = control.MaxHeight = control.MinHeight = ActualHeight*0.7;
					} else {
						control.Height = double.NaN;
						control.MinHeight = 0;
						control.MaxHeight = double.PositiveInfinity;
					}
				}

				if (control.GetType() == grid2Type ||
					control.GetType() == gridType) {
					if (!hideScrollBar) {
						control.MaxHeight = ActualHeight*0.5;
					} else {
						control.MaxHeight = double.PositiveInfinity;
					}
				}
			}

		}

		private List<Control> FindControlsByType(Type[] t)
		{
			List<Control> list = new List<Control>();

			RecurciveSearchControls(ref list, t.ToList() , this);

			return list;
		}

		private void RecurciveSearchControls(ref List<Control> controls, List<Type> t, DependencyObject control)
		{
			int childrenCount = VisualTreeHelper.GetChildrenCount(control);

			for (int i = 0; i < childrenCount; i++) {

				DependencyObject currentChildControl = VisualTreeHelper.GetChild(control, i);

				if (t.IndexOf(currentChildControl.GetType()) > -1) {

				}
				controls.Add(currentChildControl as Control);

				if (VisualTreeHelper.GetChildrenCount(currentChildControl) > 0) {
					RecurciveSearchControls(ref controls, t, currentChildControl);
				}
			}

		}
	}
}
