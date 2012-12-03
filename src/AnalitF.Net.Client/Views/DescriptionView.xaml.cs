using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Views
{
	public partial class DescriptionView : UserControl
	{
		public DescriptionView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				TryClose.Focus();
			};
		}

		private void InvokeViewModel(object sender, ExecutedRoutedEventArgs e)
		{
			ViewModelHelper.InvokeDataContext(sender, e.Parameter as string);
		}

		private void CanInvokeViewModel(object sender, CanExecuteRoutedEventArgs e)
		{
			var result = ViewModelHelper.InvokeDataContext(sender, "Can" + e.Parameter)
				?? ViewModelHelper.InvokeDataContext(sender, "get_Can" + e.Parameter);
			if (result is bool)
				e.CanExecute = (bool)result;
			else {
				e.CanExecute = true;
			}
		}
	}
}
