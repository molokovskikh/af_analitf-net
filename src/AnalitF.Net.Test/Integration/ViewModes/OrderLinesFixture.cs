using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrderLinesFixture : BaseFixture
	{
		[Test]
		public void Show()
		{
			var model = Init(new OrderLinesViewModel());
		}
	}
}