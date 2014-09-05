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
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools.Calendar;

namespace AnalitF.Net.Client.Views
{
	public partial class ShellView : Window
	{
		private object originalContent;
		private bool isNotifing;
		private Queue<string> pending = new Queue<string>();
		private DispatcherTimer notificationTimer = new DispatcherTimer();
		private double lastwidth;

		public ShellView()
		{
			InitializeComponent();
			notificationTimer.Tick += UpdateNot;
			Loaded += (sender, args) => {
				//для тестов
				var button = Update.Descendants<Button>().First(b => b.Name == "PART_ActionButton");
				button.SetValue(AutomationProperties.AutomationIdProperty, "Update");
				originalContent = data.Content;
				((ShellViewModel)DataContext).Notifications.Subscribe(n => Append(n));
			};

			EventManager.RegisterClassHandler(typeof(ShellView), Hyperlink.RequestNavigateEvent,
				new RoutedEventHandler(
					(sender, args) => {
						var url = ((RequestNavigateEventArgs)args).Uri.ToString();
						new OpenResult(url).Execute(new ActionExecutionContext());
					}));

			//проблема
			//в адресе доставки может быть много символов в этом случае он съест все кнопки тк для него установлена
			//опция которая запрещает панели инструментов скрывать его
			//опция запрещающая скрытие установлена что бы избежать ситуации когда список адресов доставки исчезает после выбора длинного адреса
			//это происходит потому что длинному адресу нехватает места и он уходит на панель переполнения
			//у вычисления максимальной ширины есть два защитных механизма которые предотвращают зацикливание алгоритма
			//вычисления местоположения элементов
			//первый - Math.Abs(ToolBar.ActualWidth - lastwidth) > 50
			//второй - ispatcher.BeginInvoke(DispatcherPriority.ContextIdle
			//если убрать хотя бы один вычисление будет производиться бесконечно
			ToolBar.SizeChanged += (sender, args) => {
				if (Math.Abs(ToolBar.ActualWidth - lastwidth) > 50)
					Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new System.Action(() => {
						lastwidth = ToolBar.ActualWidth;
						var result = ToolBar.ActualWidth / 2;
						if (!double.IsNaN(result)) {
							Addresses.MaxWidth = result;
						}
					}));
			};
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
	}
}
