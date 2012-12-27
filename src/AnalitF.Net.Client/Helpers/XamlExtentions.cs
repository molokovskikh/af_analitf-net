using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Common.Tools;

namespace AnalitF.Net.Client.Helpers
{
	public static class XamlExtentions
	{
		public static IEnumerable<DependencyObject> Children(this Visual visual)
		{
			var count = VisualTreeHelper.GetChildrenCount(visual);
			for (var i = 0; i < count; i++) {
				yield return VisualTreeHelper.GetChild(visual, i);
			}
		}

		public static T GetVisualChild<T>(this Visual parent) where T : Visual
		{
			var child = default(T);

			var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < numVisuals; i++)
			{
				var v = (Visual)VisualTreeHelper.GetChild(parent, i);
				child = v as T;
				if (child == null)
				{
					child = GetVisualChild<T>(v);
				}
				if (child != null)
				{
					break;
				}
			}

			return child;
		}

		public static bool IsVisual(this DependencyObject o)
		{
			return o is Visual || o is Visual3D;
		}

		public static IEnumerable<DependencyObject> Children(this DependencyObject o)
		{
			var visualCount = IsVisual(o) ? VisualTreeHelper.GetChildrenCount(o) : 0;
			if (visualCount > 0) {
				for (int i = 0; i < visualCount; i++) {
					yield return VisualTreeHelper.GetChild(o, i);
				}
			}
			else {
				var contentControl = o as ContentControl;
				if (contentControl != null) {
					var content = contentControl.Content as DependencyObject;
					if (content != null)
						yield return content;
				}

				foreach (var child in LogicalTreeHelper.GetChildren(o).OfType<DependencyObject>()) {
					yield return child;
				}
			}
		}

		public static IEnumerable<object> Parents(this DependencyObject o)
		{
			var parent = Parent(o);
			while (parent != null) {
				yield return parent;
				parent = Parent(parent);
			}
		}

		public static DependencyObject Parent(this DependencyObject o)
		{
			var frameworkElement = o as FrameworkElement;
			if (frameworkElement != null)
				return frameworkElement.Parent;
			return VisualTreeHelper.GetParent(o);
		}

		public static IEnumerable<DependencyObject> DeepChildren(this DependencyObject view)
		{
			return view.Children().Flat(Children);
		}
	}
}