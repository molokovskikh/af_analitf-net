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
				var checkAllItem = (ComboBoxItem)this.FindName("CheckAllItem");
				model.Settings.Where(x => x != null).Subscribe(x => {
					//если шаблон задан не нужно его переопределять это приведет к ошибкам
					if (x.EditAddresses) {
						if (!(Addresses.ItemTemplateSelector is AddressTemplateSelector)) {
							Addresses.ItemTemplateSelector = new AddressTemplateSelector(this);
						}
						if (checkAllItem != null) {
							checkAllItem.Visibility = Visibility.Visible;
						}
					} else {
						if (!(Addresses.ItemTemplateSelector is AddressTemplateSelector2)) {
							Addresses.ItemTemplateSelector = new AddressTemplateSelector2(this);
						}
						if (checkAllItem != null) {
							checkAllItem.Visibility = Visibility.Collapsed;
						}
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
