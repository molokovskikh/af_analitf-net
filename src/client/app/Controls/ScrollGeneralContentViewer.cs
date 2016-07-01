using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Linq.Expressions;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
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

	public class ScrollGeneralContentViewer: ScrollViewer
	{
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

		public ScrollGeneralContentViewer()
		{
			ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;
			SizeChanged += OnSizeChanged;
			GotFocus += OnGetFocus;
			Focusable = false;
		}

		private void OnGetFocus(object sender, RoutedEventArgs e)
		{
			if (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
			{
				OptimizateGridsForVScrolling(true);
			}
			else
			{
				OptimizateGridsForVScrolling(false);
			}

			if (e.Source is Control)
			{

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
			var window = Window.GetWindow(this);
			VerticalScrollBarVisibility = (window.ActualHeight <= ScrollActivateHeigth)
				? ScrollBarVisibility.Auto
				: ScrollBarVisibility.Disabled;
		}

		private void OptimizateGridsForVScrolling(bool hideScrollBar)
		{
			Type grid2Type = typeof(DataGrid2);
			Type gridType = typeof(DataGrid);
			Type textBox = typeof (TextBox);

			var controls = WpfHelper.Children(this, new List<Type> {grid2Type, gridType, textBox});

			if (!hideScrollBar) {
				ScrollToVerticalOffset(0);
			}

			foreach (var dependecyObj in controls) {

				if (dependecyObj == null) continue;

				var control = dependecyObj as Control;

				if (dependecyObj.GetType() == grid2Type ||
					dependecyObj.GetType() == gridType) {

					if (!hideScrollBar) {
						if (WpfHelper.Parent(control).GetType() == typeof(MainControllerWrap))
						{
							control.Height = control.MaxHeight = control.MinHeight = ActualHeight * 0.75;
							continue;
						}
						control.MaxHeight = ActualHeight*0.5;
					} else {
						if (WpfHelper.Parent(control).GetType() == typeof (MainControllerWrap)) {
							control.Height = double.NaN;
							control.MinHeight = 0;
							control.MaxHeight = double.PositiveInfinity;
							continue;
						}
						control.MaxHeight = double.PositiveInfinity;
					}
				}

				if (dependecyObj.GetType() == textBox) {
					if (!hideScrollBar) {
						control.MaxHeight = ActualHeight*0.4;
					} else {
						{
							control.MaxHeight = double.PositiveInfinity;
						}
					}
				}
			}

		}
	}
}
