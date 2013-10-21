using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class OrderLinesFixture : BaseViewFixture
	{
		[Test]
		public void Bind_address_selector()
		{
			var view = Bind(new OrderLinesViewModel());

			var all = view.Descendants<Control>().First(e => e.Name == "All");
			Assert.That(all.Visibility, Is.EqualTo(Visibility.Collapsed));
		}
	}
}