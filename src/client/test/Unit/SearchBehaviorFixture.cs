using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class SearchBehaviorFixture : BaseUnitFixture
	{
		public class TestScreen : BaseScreen
		{
		}

		[Test]
		public void Do_not_update()
		{
			var screen = new TestScreen();
			var behavior = new SearchBehavior(screen);
			behavior.SearchText.Value = "test";
			behavior.Search();
			Assert.AreEqual("", behavior.SearchText.Value);
			Assert.AreEqual("test", behavior.ActiveSearchTerm.Value);
		}
	}
}