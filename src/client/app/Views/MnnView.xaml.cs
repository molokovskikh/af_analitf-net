using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views
{
	public partial class MnnView : UserControl
	{
		public MnnView()
		{
			InitializeComponent();
			ApplyStyles();
			SearchText.KeyDown += (sender, args) => {
				if (args.Key == Key.Return) {
					DataGridHelper.Focus(Mnns);
				}
			};
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Mnn), Mnns, Application.Current.Resources);
		}
	}
}
