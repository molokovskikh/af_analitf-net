using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Controls
{
	public interface ISelectable
	{
		bool IsSelected {get; set; }
	}

	public class Selectable<T> : BaseNotify, ISelectable
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
				OnPropertyChanged();
			}
		}

		public T Item { get; set; }
	}

	public class PopupSelector : MultiSelector
	{
		public static RoutedUICommand UnselectAllCommand = new RoutedUICommand();
		public static RoutedUICommand SelectAllCommand = new RoutedUICommand();

		public static DependencyProperty MemberProperty = DependencyProperty.Register("Member",
			typeof(string),
			typeof(PopupSelector),
			new FrameworkPropertyMetadata(UpdateMember));

		static PopupSelector()
		{
			EventManager.RegisterClassHandler(typeof(PopupSelector),
				Mouse.MouseDownEvent,
				new MouseButtonEventHandler(OnMouseButtonDown),
				true);
			CommandManager.RegisterClassCommandBinding(typeof(PopupSelector),
				new CommandBinding(UnselectAllCommand, UnselectAll));
			CommandManager.RegisterClassCommandBinding(typeof(PopupSelector),
				new CommandBinding(SelectAllCommand, SelectAll));
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
			= DependencyProperty.RegisterAttached("IsOpen",
				typeof(bool),
				typeof(PopupSelector),
				new PropertyMetadata(false, IsOpenChanged));

		public static DependencyProperty ButtonContentProperty
			= DependencyProperty.RegisterAttached("ButtonContent",
				typeof(object),
				typeof(PopupSelector),
				new PropertyMetadata());

		private static void UpdateMember(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selector = (PopupSelector)d;
			selector.ItemTemplate = new DataTemplate();
			var factory = new FrameworkElementFactory(typeof(MenuItem));
			factory.SetBinding(MenuItem.HeaderProperty, new Binding(selector.Member));
			factory.SetBinding(MenuItem.IsCheckedProperty, new Binding("IsSelected"));
			factory.SetValue(MenuItem.IsCheckableProperty, true);
			selector.ItemTemplate.VisualTree = factory;
			selector.ItemTemplate.Seal();
		}

		public PopupSelector()
		{
			Member = "Item.Name";
			Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
		}

		private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		public string Member
		{
			get { return (string)GetValue(MemberProperty); }
			set { SetValue(MemberProperty, value); }
		}

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