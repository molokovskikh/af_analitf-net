using System.Linq;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class NavigationFixture
	{
		private BaseShell shell;
		private DefaultScreen defaultScreen;
		private Navigator navigation;

		public class DefaultScreen : Screen, IActivateEx
		{
			public bool IsClosed;

			public override void TryClose(bool? dialogResult)
			{
				if (!IsClosed)
					IsClosed = true;
				base.TryClose(dialogResult);
			}

			public override void TryClose()
			{
				if (!IsClosed)
					IsClosed = true;
				base.TryClose();
			}

			public bool IsSuccessfulActivated { get; private set; }

			protected override void OnActivate()
			{
				IsSuccessfulActivated = true;
				base.OnActivate();
			}

			protected override void OnDeactivate(bool close)
			{
				if (!IsClosed)
					IsClosed = close;
				base.OnDeactivate(close);
			}
		}

		[SetUp]
		public void Setup()
		{
			Init();

			ScreenExtensions.TryActivate(shell);
			navigation.Activate();
		}

		[Test]
		public void Navigate()
		{
			Assert.AreEqual(defaultScreen, shell.ActiveItem);
			var screen = new Screen();
			navigation.Navigate(screen);

			Assert.AreEqual(screen, shell.ActiveItem);
			Assert.AreEqual(0, navigation.NavigationStack.Count());
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Do_not_close_default_item()
		{
			var screen1 = new Screen();
			navigation.NavigateRoot(screen1);

			var screen2 = new Screen();
			navigation.NavigateRoot(screen2);

			Assert.AreEqual(screen1, shell.ActiveItem);
			Assert.AreEqual(0, navigation.NavigationStack.Count());
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Reset_navigation()
		{
			navigation.NavigateRoot(new Screen());
			navigation.Navigate(new Screen());
			navigation.ResetNavigation();
			Assert.AreEqual(0, navigation.NavigationStack.Count());
			Assert.AreEqual(defaultScreen, shell.ActiveItem);
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Navigate_and_reset()
		{
			var screen1 = new Screen();
			var screen2 = new Screen();
			navigation.NavigateAndReset(screen1, screen2);
			Assert.AreEqual(1, navigation.NavigationStack.Count());
			Assert.AreEqual(screen2, shell.ActiveItem);
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Navigate_root()
		{
			navigation.NavigateRoot(new Screen());
			navigation.NavigateRoot(new Screen());
			Assert.AreEqual(0, navigation.NavigationStack.Count());
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Activate_item_from_stack()
		{
			var s1 = new Screen();
			var s2 = new Screen();
			navigation.NavigateRoot(s1);
			navigation.Navigate(s2);
			Assert.AreEqual(1, navigation.NavigationStack.Count());
			Assert.AreEqual(s2, shell.ActiveItem);
			s2.TryClose();
			Assert.AreEqual(s1, shell.ActiveItem);
			s1.TryClose();
			Assert.AreEqual(defaultScreen, shell.ActiveItem);
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Null_default_screen()
		{
			navigation.DefaultScreen = null;
			var s1 = new Screen();
			navigation.NavigateRoot(s1);
			var s2 = new Screen();
			navigation.Navigate(s2);
			s2.TryClose();
			s1.TryClose();
		}

		[Test]
		public void Reset_on_not_active()
		{
			Init();
			navigation.ResetNavigation();

			ScreenExtensions.TryActivate(shell);
			Assert.IsTrue(shell.IsActive);
			Assert.IsTrue(navigation.DefaultScreen.IsActive);
		}

		private void Init()
		{
			shell = new BaseShell();
			defaultScreen = new DefaultScreen();
			navigation = shell.Navigator;
			navigation.DefaultScreen = defaultScreen;
		}
	}
}