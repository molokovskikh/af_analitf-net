using System;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Config.Caliburn;

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
		}
	}
}
