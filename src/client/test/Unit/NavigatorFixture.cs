using System;
using System.Linq;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Castle.Components.DictionaryAdapter;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class NavigatorFixture
	{
		private BaseShell shell;
		private DefaultScreen defaultScreen;
		private Navigator navigator;

		public class TestScreen : Screen, IDisposable
		{
			public bool IsDisposed;

			protected override void OnDeactivate(bool close)
			{
				if (close)
					Dispose();
				base.OnDeactivate(close);
			}

			public void Dispose()
			{
				IsDisposed = true;
			}
		}

		public class Screen1 : TestScreen
		{
		}

		public class Screen2 : TestScreen
		{
		}

		public class Screen3 : TestScreen
		{
		}

		public class DefaultScreen : Screen, IActivateEx
		{
			public bool IsClosed;
			public bool IsDisposed;

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
				if (!IsClosed) {
					IsClosed = close;
					IsDisposed = true;
				}
				base.OnDeactivate(close);
			}
		}

		[SetUp]
		public void Setup()
		{
			Init();

			ScreenExtensions.TryActivate(shell);
			navigator.Activate();
		}

		[Test]
		public void Navigate()
		{
			Assert.AreEqual(defaultScreen, shell.ActiveItem);
			var screen = new Screen();
			navigator.Navigate(screen);

			Assert.AreEqual(screen, shell.ActiveItem);
			Assert.AreEqual(0, navigator.NavigationStack.Count());
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Do_not_close_default_item()
		{
			var screen1 = new Screen1();
			navigator.NavigateRoot(screen1);

			var screen2 = new Screen2();
			navigator.NavigateRoot(screen2);

			Assert.AreEqual(screen2, shell.ActiveItem);
			Assert.AreEqual(0, navigator.NavigationStack.Count());
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Reset_navigation()
		{
			navigator.NavigateRoot(new Screen());
			navigator.Navigate(new Screen());
			navigator.ResetNavigation();
			Assert.AreEqual(0, navigator.NavigationStack.Count());
			Assert.AreEqual(defaultScreen, shell.ActiveItem);
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Navigate_and_reset()
		{
			var screen1 = new Screen1();
			var screen2 = new Screen2();
			navigator.NavigateAndReset(screen1, screen2);
			Assert.AreEqual(1, navigator.NavigationStack.Count());
			Assert.AreEqual(screen2, shell.ActiveItem);
			Assert.IsFalse(defaultScreen.IsClosed);
		}

		[Test]
		public void Activate_item_from_stack()
		{
			var s1 = new Screen1();
			var s2 = new Screen2();
			navigator.NavigateRoot(s1);
			navigator.Navigate(s2);
			Assert.AreEqual(1, navigator.NavigationStack.Count());
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
			navigator.DefaultScreen = null;
			var s1 = new Screen1();
			navigator.NavigateRoot(s1);
			var s2 = new Screen2();
			navigator.Navigate(s2);
			s2.TryClose();
			s1.TryClose();
		}

		[Test]
		public void Reset_on_not_active()
		{
			Init();
			navigator.ResetNavigation();

			ScreenExtensions.TryActivate(shell);
			Assert.IsTrue(shell.IsActive);
			Assert.IsTrue(navigator.DefaultScreen.IsActive);
		}

		[Test]
		public void Close_items_from_stack()
		{
			var s1 = new Screen1();
			var s2 = new Screen2();
			var s3 = new Screen3();
			navigator.NavigateRoot(s1);
			navigator.Navigate(s2);
			navigator.NavigateRoot(s3);
			Assert.IsTrue(s2.IsDisposed);
			Assert.IsTrue(s1.IsDisposed);
		}

		[Test]
		public void Reactivate_item_from_stack()
		{
			var s1 = new Screen1();
			var s2 = new Screen2();
			var s3 = new Screen2();
			var s11 = new Screen1();
			navigator.NavigateRoot(s1);
			navigator.Navigate(s2);
			navigator.Navigate(s3);
			navigator.NavigateRoot(s11);

			//при попытки активировать форму которая уже есть в стеке
			//возвращаемся к форме из стека
			Assert.IsTrue(s2.IsDisposed);
			Assert.IsTrue(s3.IsDisposed);

			Assert.IsFalse(s11.IsActive);
			Assert.IsTrue(s11.IsDisposed);

			Assert.IsFalse(s1.IsDisposed);
			Assert.IsTrue(s1.IsActive);
			Assert.AreEqual(shell.ActiveItem, s1);
		}

		[Test]
		public void Release_resources()
		{
			var s1 = new Screen1();
			var s2 = new Screen2();
			navigator.NavigateRoot(s1);
			navigator.Navigate(s2);
			var s3 = new Screen3();
			var s21 = new Screen2();
			navigator.NavigateAndReset(s3, s21);
			navigator.NavigateRoot(new Screen1());
			Assert.IsTrue(s1.IsDisposed);
			Assert.IsTrue(s2.IsDisposed);
			Assert.IsTrue(s3.IsDisposed);
			Assert.IsTrue(s21.IsDisposed);
		}

		private void Init()
		{
			shell = new BaseShell();
			defaultScreen = new DefaultScreen();
			navigator = shell.Navigator;
			navigator.DefaultScreen = defaultScreen;
		}
	}
}