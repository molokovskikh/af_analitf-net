using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.ViewModels
{
	public class DescriptionViewModel : BaseScreen
	{
		public DescriptionViewModel(ProductDescription description)
		{
			Description = description;
		}

		public ProductDescription Description { get; set; }
	}
}