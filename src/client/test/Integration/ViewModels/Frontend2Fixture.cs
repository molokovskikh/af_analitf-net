using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Inventory;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class Frontend2Fixture : ViewModelFixture
	{
		[Test]
		public void Close()
		{
			Frontend2 model = null;
			model.Close()
		}
	}
}