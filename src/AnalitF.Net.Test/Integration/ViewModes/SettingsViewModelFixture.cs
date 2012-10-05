using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SettingsViewModelFixture : BaseFixture
	{
		[Test]
		public void Calculate_base_category()
		{
			var model = Init(new SettingsViewModel());
			model.Settings.GroupByProduct = !model.Settings.GroupByProduct;
			model.Save();
		}
	}
}