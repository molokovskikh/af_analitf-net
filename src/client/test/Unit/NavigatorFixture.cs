﻿using System;
using System.Linq;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class NavigatorFixture : BaseUnitFixture
	{
		private BaseShell shell;
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

		[SetUp]
		public void Setup()
		{
			Init();

			ScreenExtensions.TryActivate(shell);
		}

		[Test]
		public void Navigate()
		{
			var screen = new Screen();
			navigator.Navigate(screen);

			Assert.AreEqual(screen, shell.ActiveItem);
			Assert.AreEqual(1, navigator.NavigationStack.Count());
		}

		[Test]
		public void Navigate_and_reset()
		{
			var screen1 = new Screen1();
			var screen2 = new Screen2();
			navigator.NavigateAndReset(screen1, screen2);
			Assert.AreEqual(2, navigator.NavigationStack.Count());
			Assert.AreEqual(screen2, shell.ActiveItem);
		}

		[Test]
		public void Navigate_root()
		{
			var s1 = new Screen1();
			var s2 = new Screen2();
			var s3 = new Screen3();
			navigator.NavigateRoot(s1);
			navigator.Navigate(s2);
			navigator.NavigateRoot(s3);
			Assert.AreEqual(shell.ActiveItem, s3);
		}

		private void Init()
		{
			shell = new BaseShell();
			shell.Navigator = new Navigator(shell);
			navigator = (Navigator)shell.Navigator;
		}
	}
}