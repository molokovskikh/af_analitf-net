﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
	public interface ISelectable : INotifyPropertyChanged
	{
		bool IsSelected {get; set; }
	}

	public static class SelectableHelper
	{
		public static IObservable<EventPattern<PropertyChangedEventArgs>> FilterChanged<T>(this NotifyValue<IList<Selectable<T>>> selectable)
		{
			return selectable.SelectMany(p => p?.Select(x => x.Changed()).Merge() ?? Observable.Empty<EventPattern<PropertyChangedEventArgs>>());
		}

		public static bool IsFiltred<T>(this NotifyValue<IList<Selectable<T>>> selectable)
		{
			if (selectable.Value == null)
				return false;
			return selectable.Value.Count != selectable.Value.Count(x => x.IsSelected);
		}

		public static T[] GetValues<T>(this NotifyValue<IList<Selectable<T>>> selectable)
		{
			if (selectable.Value == null)
				return new T[0];
			return selectable.Value.Where(x => x.IsSelected).Select(x => x.Item).ToArray();
		}
	}

	public class Selectable<T> : BaseNotify, ISelectable
	{
		private bool isSelected;

		public Selectable(T item)
			: this(item, null)
		{
		}

		public Selectable(T item, string name)
		{
			Item = item;
			Name = name;
			isSelected = true;
		}

		public string Name { get; set; }

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

	public class PopupButton : ContentControl
	{
		public static DependencyProperty ButtonContentProperty
			= DependencyProperty.RegisterAttached("ButtonContent",
				typeof(object),
				typeof(PopupButton),
				new PropertyMetadata());

		public static DependencyProperty IsOpenProperty
			= DependencyProperty.RegisterAttached("IsOpen",
				typeof(bool),
				typeof(PopupButton),
				new PropertyMetadata(false, IsOpenChanged));

		static PopupButton()
		{
			EventManager.RegisterClassHandler(typeof(PopupButton),
				Mouse.MouseDownEvent,
				new MouseButtonEventHandler(OnMouseButtonDown),
				true);
		}

		public PopupButton()
		{
			Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
		}

		public string ButtonContent
		{
			get { return (string)GetValue(ButtonContentProperty); }
			set { SetValue(ButtonContentProperty, value); }
		}

		public bool IsOpened
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty, value); }
		}

		private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			var element = (PopupButton)sender;
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

		private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		private static void IsOpenChanged(DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			if ((bool)args.NewValue) {
				Mouse.Capture((IInputElement)sender, CaptureMode.SubTree);
			}
			else {
				Mouse.Capture(null);
			}
		}
	}

	public class PopupSelector : MultiSelector
	{
		public static RoutedUICommand UnselectAllCommand = new RoutedUICommand();
		public static RoutedUICommand SelectAllCommand = new RoutedUICommand();

		public static DependencyProperty MemberProperty = DependencyProperty.Register("Member",
			typeof(string),
			typeof(PopupSelector),
			new FrameworkPropertyMetadata(UpdateMember));

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

		public static DependencyProperty IsFiltredProperty
			= DependencyProperty.RegisterAttached("IsFiltred",
				typeof(bool),
				typeof(PopupSelector),
				new PropertyMetadata(false));

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
			VerticalAlignment = VerticalAlignment.Center;
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

		public bool IsFiltred
		{
			get { return (bool)GetValue(IsFiltredProperty); }
			set { SetValue(IsFiltredProperty, value); }
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			if (oldValue != null)
				oldValue.OfType<ISelectable>().Each(s => s.PropertyChanged -= UpdateIsFiltred);
			if (newValue != null)
				newValue.OfType<ISelectable>().Each(s => s.PropertyChanged += UpdateIsFiltred);
			UpdateIsFiltred(null, null);
		}

		private void UpdateIsFiltred(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			IsFiltred = Items.Cast<ISelectable>().Count(i => i.IsSelected) != Items.Cast<ISelectable>().Count();
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