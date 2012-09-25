using AnalitF.Net.Client.Models;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels
{
	public class DescriptionViewModel : BaseScreen
	{
		public DescriptionViewModel(ProductDescription description)
		{
			Description = description;
			DisplayName = "Описание";
		}

		public ProductDescription Description { get; set; }

		public void Exit()
		{
			TryClose();
		}
	}
}