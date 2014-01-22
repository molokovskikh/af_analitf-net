﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Views
{
	public partial class Mails : UserControl
	{
		public Mails()
		{
			InitializeComponent();

			Items.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
			Term.CommandBindings.Add(new CommandBinding(Commands.CleanText, (sender, args) => {
				var box = ((DependencyObject)args.OriginalSource).VisualParents<TextBox>().FirstOrDefault();
				if (box != null) {
					box.Text = "";
				}
			}));
		}
	}
}