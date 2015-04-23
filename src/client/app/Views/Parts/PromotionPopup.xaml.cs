using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Config.Caliburn;

namespace AnalitF.Net.Client.Views.Parts
{
	public partial class PromotionPopup : UserControl
	{
		public PromotionPopup()
		{
			InitializeComponent();
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			ViewModelHelper.InvokeDataContext(this, "Open", ((FrameworkContentElement)sender).DataContext);
		}
	}
}
