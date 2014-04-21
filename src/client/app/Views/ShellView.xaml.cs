﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
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
