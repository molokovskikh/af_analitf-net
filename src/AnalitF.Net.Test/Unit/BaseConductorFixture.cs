using System;
using System.Runtime.InteropServices;
using AnalitF.Net.Client.Binders;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class BaseConductorFixture
	{
		private BaseConductor conductor;

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
			conductor = new BaseConductor();
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