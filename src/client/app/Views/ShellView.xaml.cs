using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools.Calendar;
using VerticalAlignment = System.Windows.VerticalAlignment;
using System.Globalization;
using AnalitF.Net.Client.Models;
using System.ComponentModel;

namespace AnalitF.Net.Client.Views
{
	public class AddressTemplateSelector : DataTemplateSelector
	{
		Window window;

		public AddressTemplateSelector(Window window)
		{
			this.window = window;
		}

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var parent = ((FrameworkElement)container).TemplatedParent;
			if (parent is ComboBox)
				return (DataTemplate)window.FindResource("AddressTemplate2");
			return (DataTemplate)window.FindResource("AddressTemplate");
		}
	}

	public class AddressTemplateSelector2 : DataTemplateSelector
	{
		Window window;

		public AddressTemplateSelector2(Window window)
		{
			this.window = window;
		}

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var parent = ((FrameworkElement)container).TemplatedParent;
			if (parent is ComboBox)
				return (DataTemplate)window.FindResource("AddressTemplate2");
			return (DataTemplate)window.FindResource("AddressTemplate2");
		}
	}

	public class AddressVisiblityConverterPositive : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() != typeof(AddressProxy))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class AddressVisiblityConverterNegative : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.GetType() == typeof(AddressProxy))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class AddressButton
	{
		public enum Behaviors
		{
			SelectAll = 0,
			DeselectAll = 1
		};

		public string Name { get; set; }
		private Behaviors Behavior;
		private IEnumerable<AddressProxy> _addresses;

		/*
		 * Заглушка, чтобы избежать ошибок при байдинге
		 */
		public bool IsSelected { get; set; }

		public AddressButton(string caption, Behaviors behavior, IEnumerable<AddressProxy> addresses)
		{
			Name = caption;
			this.Behavior = behavior;
			this._addresses = addresses;
		}

		public void Click()
		{
			foreach (var item in _addresses)
				{
				switch (Behavior)
				{
					case Behaviors.SelectAll:
						item.IsSelected = true;
						break;
					case Behaviors.DeselectAll:
						item.IsSelected = false;
						break;
				}
			}
		}
	}

	public class AddressProxy : Address, INotifyPropertyChanged
	{
		private Address _address;

		public bool IsSelected
		{
			get
			{
				if(_address.Config == null)
				{
					return false;
				}

				return _address.Config.IsActive;
			}
			set
			{
				if (_address.Config == null)
				{
					return;
				}

				_address.Config.IsActive = value;

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
			}
		}

		public AddressProxy(Address address)
		{
			_address = address;
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			var properties = address.GetType().GetProperties(bindingFlags);
			foreach(var property in properties)
			{
				if(property.GetSetMethod() == null)
				{
					continue;
				}
				property.SetValue(this, property.GetValue(address, null), null);
			}

			var fields = address.GetType().GetFields(bindingFlags);
			foreach(var field  in fields)
			{
				field.SetValue(this, field.GetValue(address));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/*
	 * Заглушка, чтобы избежать ошибок при байдинге
	 */
		public void Click() { }
	}


	public partial class ShellView : Window
	{
		private object originalContent;
		private bool isNotifing;
		private Queue<string> pending = new Queue<string>();
		private DispatcherTimer notificationTimer = new DispatcherTimer();

		public ShellView()
		{
			InitializeComponent();
			Addresses.ItemTemplateSelector = new AddressTemplateSelector2(this);
			notificationTimer.Tick += UpdateNot;
			Loaded += (sender, args) => {
				//для тестов
				var button = Update.Descendants<Button>().First(b => b.Name == "PART_ActionButton");
				button.SetValue(AutomationProperties.AutomationIdProperty, "Update");
				originalContent = data.Content;
				((ShellViewModel)DataContext).Notifications.Subscribe(n => Append(n));
			};

			DataContextChanged += (sender, args) => {
				var model = (ShellViewModel)DataContext;
				model.Settings.Where(x => x != null).Subscribe(x => {
					//если шаблон задан не нужно его переопределять это приведет к ошибкам
					if (x.EditAddresses) {
						if (!(Addresses.ItemTemplateSelector is AddressTemplateSelector)) {
							Addresses.ItemTemplateSelector = new AddressTemplateSelector(this);
						}						
												
					} else {
						if (!(Addresses.ItemTemplateSelector is AddressTemplateSelector2)) {
							Addresses.ItemTemplateSelector = new AddressTemplateSelector2(this);							
						}						
					}

					if (model.Addresses.Count == 0)
					{
						model.PropertyChanged += (sender_, e) =>
						{
							if (e.PropertyName == nameof(model.Addresses))
							{
								reloadAddresses(model, x.EditAddresses);
							}							
						};
					}
					else
					{
						reloadAddresses(model, x.EditAddresses);
					}
				});
			};

			EventManager.RegisterClassHandler(typeof(ShellView), Hyperlink.RequestNavigateEvent,
				new RoutedEventHandler(
					(sender, args) => {
						var url = ((RequestNavigateEventArgs)args).Uri.ToString();
						new OpenResult(url).Execute(new ActionExecutionContext());
					}));
#if DEBUG
			InputBindings.Add(new KeyBinding(Commands.InvokeViewModel, new KeyGesture(Key.D, ModifierKeys.Control)) {
				CommandParameter = "ShowDebug"
			});
#endif
#if !DEBUG
			Snoop.Visibility = Visibility.Collapsed;
			Collect.Visibility = Visibility.Collapsed;
			Debug_ErrorCount.Visibility = Visibility.Collapsed;
			ShowDebug.Visibility = Visibility.Collapsed;
			DebugErrorHolder.Content = null;
			DebugSqlHolder.Content = null;
#endif
		}

		private void reloadAddresses(ShellViewModel model, bool manageAddresses)
		{
			Addresses.ItemsSource = null;
			Addresses.Items.Clear();			

			var addressesProxy = new List<AddressProxy>();

			foreach (var address in model.Addresses)
			{
				addressesProxy.Add(new AddressProxy(address));
			}

			if (manageAddresses)
			{
				Addresses.Items.Add(new AddressButton("Выбрать все", AddressButton.Behaviors.SelectAll, addressesProxy));
				Addresses.Items.Add(new AddressButton("Сбросить все", AddressButton.Behaviors.DeselectAll, addressesProxy));
			}			

			foreach (var addressProxy in addressesProxy)
			{
				Addresses.Items.Add(addressProxy);
			}

			Addresses.SelectedItem = (addressesProxy.FirstOrDefault(a => a.Id == model.CurrentAddress?.Id));
		}

		private void UpdateNot(object sender, EventArgs e)
		{
			if (pending.Count == 0) {
				isNotifing = false;
				notificationTimer.Stop();
				data.Content = originalContent;
			}
			else {
				data.Content = pending.Dequeue();
			}
		}

		private void Append(string message)
		{
			if (isNotifing) {
				pending.Enqueue(message);
				return;
			}
			isNotifing = true;
			data.Content = new TextBlock {
				Width = ((FrameworkElement)originalContent).ActualWidth,
				Margin = new Thickness(5),
				Text = message
			};
			notificationTimer.Interval = 3.Second();
			notificationTimer.Start();
		}

		private void CloneClick(object sender, RoutedEventArgs e)
		{
			((ShellViewModel)DataContext).OpenClone(((string)((MenuItem)e.OriginalSource).DataContext));
		}
	}
}
