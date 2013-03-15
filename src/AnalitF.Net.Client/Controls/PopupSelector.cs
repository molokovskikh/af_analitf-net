using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Common.Tools;

namespace AnalitF.Net.Client.Controls
{
	public interface ISelectable
	{
		bool IsSelected {get; set; }
	}

	public class Selectable<T> : ISelectable, INotifyPropertyChanged
	{
		private bool isSelected;

		public Selectable(T item)
		{
			Item = item;
			isSelected = true;
		}

		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}

		public T Item { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class PopupSelector : MultiSelector
	{
		public static RoutedUICommand UnselectAllCommand = new RoutedUICommand();
		public static RoutedUICommand SelectAllCommand = new RoutedUICommand();

		static PopupSelector()
		{
			EventManager.RegisterClassHandler(typeof(PopupSelector), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
			CommandManager.RegisterClassCommandBinding(typeof(PopupSelector), new CommandBinding(UnselectAllCommand, UnselectAll));
			CommandManager.RegisterClassCommandBinding(typeof(PopupSelector), new CommandBinding(SelectAllCommand, SelectAll));
		}

		private static void UnselectAll(object sender, ExecutedRoutedEventArgs args)
		{
			((MultiSelector)sender).ItemsSource.Cast<ISelectable>().Each(s => s.IsSelected = false);
		}

		private static void SelectAll(object sender, ExecutedRoutedEventArgs args)
		{
			((MultiSelector)sender).ItemsSource.Cast<ISelectable>().Each(s => s.IsSelected = true);
		}

		public static DependencyProperty IsOpenProperty
			= DependencyProperty.RegisterAttached("IsOpen", typeof(bool), typeof(PopupSelector), new PropertyMetadata(false, IsOpenChanged));

		public static DependencyProperty ButtonContentProperty
			= DependencyProperty.RegisterAttached("ButtonContent", typeof(object), typeof(PopupSelector), new PropertyMetadata());

		public bool IsOpened
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty, value); }
		}

		public string ButtonContent
		{
			get { return (string)GetValue(ButtonContentProperty); }
			set { SetValue(ButtonContentProperty, value); }
		}

		private static void IsOpenChanged(DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var sel = (PopupSelector)sender;
			if ((bool)args.NewValue) {
				Mouse.Capture(sel, CaptureMode.SubTree);
			}
			else {
				Mouse.Capture(null);
			}
		}

		private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			var element = (PopupSelector)sender;

			if (Mouse.Captured == element && e.OriginalSource == element)  {
				element.Close();
			}
		}

		public void Close()
		{
			if (IsOpened) {
				IsOpened = false;
			}
		}
	}
}