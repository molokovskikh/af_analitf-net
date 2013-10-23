using System.Linq;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class NavigationFixture
	{
		public class DefaultScreen : Screen
		{
		}

		[Test]
		public void Navigate()
		{
			var conductor = new Conductor<IScreen>();
			var defaultScreen = new DefaultScreen();
			var navigation = new Navigator(conductor, defaultScreen);
			navigation.Activate();
			Assert.AreEqual(defaultScreen, conductor.ActiveItem);
			var screen = new Screen();
			navigation.Navigate(screen);
			Assert.AreEqual(screen, conductor.ActiveItem);
			Assert.AreEqual(0, navigation.NavigationStack.Count());
		}
	}
}