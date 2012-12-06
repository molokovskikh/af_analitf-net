using System.Windows.Controls;

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
	}
}
