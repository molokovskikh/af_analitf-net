using System;
using AnalitF.Net.Client.Config.Caliburn;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class BaseConductorFixture
	{
		private BaseShell conductor;

		public class TestScreen : Screen, IActivateEx
		{
			public bool IsSuccessfulActivated { get; private set; }

			protected override void OnActivate()
			{
				IsSuccessfulActivated = false;
			}
		}

		[SetUp]
		public void Setup()
		{
			conductor = new BaseShell();
			ScreenExtensions.TryActivate(conductor);
		}

		[Test]
		public void Screen_can_reject_item_activation()
		{
			var testScreen = new TestScreen();
			conductor.ActivateItem(testScreen);
			Assert.That(conductor.ActiveItem, Is.Null);
			Assert.That(testScreen.IsActive, Is.False);
		}
	}
}