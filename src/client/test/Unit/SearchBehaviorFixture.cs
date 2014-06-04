using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class SearchBehaviorFixture : BaseUnitFixture
	{
		public class TestScreen : BaseScreen
		{
			public int Updated;

			public override void Update()
			{
				Updated++;
			}
		}

		[Test]
		public void Do_not_update()
		{
			var screen = new TestScreen();
			var behavior = new SearchBehavior(screen, callUpdate: false);
			behavior.SearchText.Value = "test";
			behavior.Search();
			Assert.AreEqual(0, screen.Updated);
			Assert.AreEqual("", behavior.SearchText.Value);
			Assert.AreEqual("test", behavior.ActiveSearchTerm.Value);
		}
	}
}