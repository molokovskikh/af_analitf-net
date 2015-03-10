using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using Common.Tools;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Helpers
{
	public static class WpfHelper
	{
		private class PropertyLogger
		{
			private DependencyProperty Property;
			private DependencyObject O;
			private bool stack;

			public PropertyLogger(DependencyObject o, DependencyProperty property, bool stack = false)
			{
				O = o;
				Property = property;
				this.stack = stack;
			}

			public object Logger
			{
				get { return null; }
				set
				{
					Console.WriteLine("{1}.{2} = {0}",
						value,
						((FrameworkElement)O).Name,
						Property.Name);
					if (stack) {
						Console.WriteLine(new StackTrace());
					}
				}
			}
		}

		public static void AddRange(this UIElementCollection dst, IEnumerable<UIElement> src)
		{
			foreach (var element in src) {
				dst.Add(element);
			}
		}

		public static IEnumerable<DependencyObject> Children(this Visual visual)
		{
			var count = VisualTreeHelper.GetChildrenCount(visual);
			for (var i = 0; i < count; i++) {
				yield return VisualTreeHelper.GetChild(visual, i);
			}
		}

		public static T VisualChild<T>(this Visual parent) where T : Visual
		{
			var child = default(T);

			var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < numVisuals; i++) {
				var v = (Visual)VisualTreeHelper.GetChild(parent, i);
				child = v as T;
				if (child == null) {
					child = VisualChild<T>(v);
				}
				if (child != null) {
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
				var haveLogicalChildren = false;
				foreach (var child in LogicalTreeHelper.GetChildren(o).OfType<DependencyObject>()) {
					haveLogicalChildren = true;
					yield return child;
				}

				if (!haveLogicalChildren) {
					var contentControl = o as ContentControl;
					if (contentControl != null) {
						var content = contentControl.Content as DependencyObject;
						if (content != null)
							yield return content;
					}
				}
			}
		}

		public static IEnumerable<T> Parents<T>(this DependencyObject o)
		{
			return Parents(o).OfType<T>();
		}

		public static IEnumerable<object> Parents(this DependencyObject o)
		{
			var parent = Parent(o);
			while (parent != null) {
				yield return parent;
				parent = Parent(parent);
			}
		}

		public static IEnumerable<T> VisualParents<T>(this DependencyObject o)
		{
			return o.VisualParents().OfType<T>();
		}

		public static IEnumerable<DependencyObject> VisualParents(this DependencyObject o)
		{
			if (!(o is Visual) && !(o is Visual3D))
				yield break;

			var parent = VisualParent(o);
			while (parent != null) {
				yield return parent;
				parent = VisualParent(parent);
			}
		}

		public static DependencyObject VisualParent(DependencyObject o)
		{
			if (o is Visual || o is Visual3D)
				return VisualTreeHelper.GetParent(o);
			return null;
		}

		public static DependencyObject Parent(this DependencyObject o)
		{
			var frameworkElement = o as FrameworkElement;
			if (frameworkElement != null)
				return frameworkElement.Parent;
			var content = o as FrameworkContentElement;
			if (content != null)
				return content.Parent;
			if (o is Visual || o is Visual3D)
				return VisualTreeHelper.GetParent(o);
			return null;
		}

		public static IEnumerable<DependencyObject> Descendants(this DependencyObject view)
		{
			return view.Children().Flat(Children);
		}

		public static IEnumerable<T> Siblings<T>(this DependencyObject view)
		{
			var parent = view.Parent();
			if (parent == null)
				return Enumerable.Empty<T>();
			return parent.Children().Except(new[] { view }).OfType<T>();
		}

		public static IEnumerable<T> Descendants<T>(this DependencyObject view)
		{
			return view.Descendants().OfType<T>();
		}

		public static void PrintVisualTree(Visual visual, int offset = 0)
		{
			for (var i = 0; i < offset; i++)
				Console.Write("  ");
			if (visual is TextBlock)
				Console.WriteLine("{0} {1}", visual, ((TextBlock)visual).Text);
			else if (visual is FrameworkElement)
				Console.WriteLine("{0} {1}", visual, ((FrameworkElement)visual).Name);
			else
				Console.WriteLine(visual);
			var count = VisualTreeHelper.GetChildrenCount(visual);
			for (var i = 0; i < count; i++) {
				PrintVisualTree((Visual)VisualTreeHelper.GetChild(visual, i), offset + 1);
			}
		}

		public static string ToText(DependencyObject item)
		{
			if (item is TextBlock) {
				return ((TextBlock)item).Text;
			}
			if (item is TextBox) {
				return ((TextBox)item).Text;
			}
			if (item is ContentControl && ((ContentControl)item).Content is string) {
				return (string)((ContentControl)item).Content;
			}
			return null;
		}

		public static string AsText(this DependencyObject item)
		{
			Func<string> getter = () => item.Descendants()
				.Where(c => !(c is UIElement) || ((UIElement)c).Visibility == Visibility.Visible)
				.Select(ToText)
				.Where(c => c != null)
				.Implode(Environment.NewLine);
			if (item.Dispatcher.Thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId) {
				var inner = getter;
				getter = () => {
					var result = "";
					item.Dispatcher.Invoke(new Action(() => {
						result = inner();
					}));
					return result;
				};
			}
			return getter();
		}

		/// <summary>
		/// предназначен для отладки, протоколирует события
		/// пример WpfHelper.TraceEvent(typeof(UIElement), UIElement.GotFocusEvent, trace: true);
		/// </summary>
		public static void TraceEvent(Type type, RoutedEvent @event, Func<EventArgs, object> map = null, bool trace = false)
		{
			EventManager.RegisterClassHandler(type, @event,
				new RoutedEventHandler(
					(sender, args) => {
						Console.WriteLine("{0} {1}.{2} {3} {4}",
							@event.Name,
							((FrameworkElement)sender).Name,
							sender,
							map != null ? map(args) : args,
							trace ? new StackTrace() : null);
					}));
		}

		/// <summary>
		/// предназначен для отладки, протоколирует изменения свойств
		/// </summary>
		public static void TraceProperty(DependencyObject o, DependencyProperty property, bool stack = false)
		{
			BindingOperations.SetBinding(o, property, new Binding("Logger") {
				Source = new PropertyLogger(o, property, stack),
				BindsDirectlyToSource = true,
				Mode = BindingMode.OneWayToSource
			});
		}

		public static void DumpStyle(Control control)
		{
			var settings = new XmlWriterSettings { Indent = true, NewLineOnAttributes = true };
			XamlWriter.Save(control.Template, XmlWriter.Create(Console.Out, settings));
			XamlWriter.Save(control.Style, XmlWriter.Create(Console.Out, settings));
		}

		public static IObservable<EventPattern<TextCompositionEventArgs>> ObservableTextInput(this UIElement el)
		{
			return Observable.FromEventPattern<TextCompositionEventArgs>(el, "TextInput");
		}
	}
}