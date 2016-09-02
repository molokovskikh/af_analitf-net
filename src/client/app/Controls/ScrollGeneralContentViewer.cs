using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Xceed.Wpf.Toolkit.Core.Converters;

namespace AnalitF.Net.Client.Controls
{
	public class ScrollGeneralContentViewer: ScrollViewer
	{
		protected readonly double ScrollActivateHeigth = 600;
		public bool IsScrolling {get ; private set;}

		public ScrollGeneralContentViewer()
		{
			SizeChanged += (sender, e) => { OptimizateContentForVScrolling(); };
			GotFocus += (sender, e) => {
				OptimizateContentForVScrolling();
			};
			Focusable = false;
		}

		private void OptimizateContentForVScrolling()
		{
			Type grid2Type = typeof(DataGrid2);
			Type gridType = typeof(DataGrid);
			Type textBox = typeof (TextBox);
			Type mainControllerWrap = typeof (MainControllerWrap);

			var controls = WpfHelper.Children(this, new List<Type> {grid2Type, gridType, textBox, mainControllerWrap}).ToList();

			var hideScrollBar = !(controls.Any(c => c.GetType() == mainControllerWrap) && getWindowHeigth() <= ScrollActivateHeigth);
			VerticalScrollBarVisibility = hideScrollBar ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

			if (hideScrollBar == !IsScrolling)
			{
				return;
			}

			if (!hideScrollBar)
			{
				ScrollToVerticalOffset(0);
			}

			foreach (var dependecyObj in controls) {

				var control = dependecyObj as Control;

				if (control == null)
				{
					continue;
				}

				if (control.GetType() == grid2Type ||
					control.GetType() == gridType) {
					var a = control.Name;
					if (!hideScrollBar) {
						(control as DataGrid).HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
						if (WpfHelper.Parent(control).GetType() == typeof(MainControllerWrap))
						{
							control.Height = control.MaxHeight = control.MinHeight = ActualHeight * 0.65;
							continue;
						}
						control.MaxHeight = control.Height = ActualHeight*0.5;
					} else {
						(control as DataGrid).HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
						if (WpfHelper.Parent(control).GetType() == typeof (MainControllerWrap)) {
							control.Height = double.NaN;
							control.MinHeight = 0;
							control.MaxHeight = double.PositiveInfinity;
							continue;
						}
						control.MaxHeight = double.PositiveInfinity;
					}
				}

				if (control.GetType() == textBox) {
					if (!hideScrollBar) {
						control.MaxHeight = ActualHeight*0.4;
					} else {
						{
							control.MaxHeight = double.PositiveInfinity;
						}
					}
				}
			}
			IsScrolling = !hideScrollBar;
		}

		private int getWindowHeigth()
		{
			var rect = new WinApi.RECT();
			IntPtr windowHandle = new WindowInteropHelper(Window.GetWindow(this)).Handle;
			WinApi.GetWindowRect(new HandleRef(this, windowHandle), out rect);

			return rect.Bottom - rect.Top;
		}
	}
}
