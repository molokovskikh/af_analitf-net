using System;
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
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;

namespace AnalitF.Net.Client.Views.Dialogs
{
	/// <summary>
	/// Interaction logic for RegistryDocSettings.xaml
	/// </summary>
	public partial class RegistryDocSettings : UserControl
	{
		public RegistryDocSettings()
		{
			InitializeComponent();
			SignerType.SelectionChanged += (sender, args) => {
				var value = SignerType.SelectedValue as ValueDescription;
				if ((RegistryDocumentSettings.SignerType)value.Value == RegistryDocumentSettings.SignerType.Acceptor) {
					Acceptor.Visibility = Visibility.Visible;
					AcceptorLabel.Visibility = Visibility.Visible;
					CommitteeMember1.Visibility = Visibility.Collapsed;
					CommitteeMember2.Visibility = Visibility.Collapsed;
					CommitteeMember3.Visibility = Visibility.Collapsed;
					CommitteeMemberLabel.Visibility = Visibility.Collapsed;
				} else {
					Acceptor.Visibility = Visibility.Collapsed;
					AcceptorLabel.Visibility = Visibility.Collapsed;
					CommitteeMember1.Visibility = Visibility.Visible;
					CommitteeMember2.Visibility = Visibility.Visible;
					CommitteeMember3.Visibility = Visibility.Visible;
					CommitteeMemberLabel.Visibility = Visibility.Visible;
				}
			};
		}
	}
}
