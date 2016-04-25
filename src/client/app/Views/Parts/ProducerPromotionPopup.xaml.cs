using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Config.Caliburn;

namespace AnalitF.Net.Client.Views.Parts
{
	/// <summary>
	/// Логика взаимодействия для ProducerPromotionPopup.xaml
	/// </summary>
	public partial class ProducerPromotionPopup : UserControl
	{
		public ProducerPromotionPopup()
		{
			InitializeComponent();
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			ViewModelHelper.InvokeDataContext(this, "Open", ((FrameworkContentElement)sender).DataContext);
		}

	}
}
