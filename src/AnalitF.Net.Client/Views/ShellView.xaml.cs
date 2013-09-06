﻿using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;

namespace AnalitF.Net.Client.Views
{
	public partial class ShellView : Window
	{
		public ShellView()
		{
			InitializeComponent();
#if !DEBUG
			Snoop.Visibility = Visibility.Collapsed;
			Collect.Visibility = Visibility.Collapsed;
			Debug_ErrorCount.Visibility = Visibility.Collapsed;
			ShowDebug.Visibility = Visibility.Collapsed;
#endif
		}
	}
}
