using AnalitF.Net.Client.Views;
using AnalitF.Net.Test.Integration.ViewModes;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA, Ignore]
	public class CatalogViewFixture : BaseFixture
	{
		[Test]
		public void Open_shell()
		{
			var view = new ShellView();
		}
	}
}