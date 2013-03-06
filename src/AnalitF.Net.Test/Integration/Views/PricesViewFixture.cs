using System;
using System.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class PricesViewFixture : BaseViewFixture
	{
		[Test]
		public void Bind_quick_search()
		{
			var model = Init(new PriceViewModel());
			var view = InitView(model);
			ForceBinding(view);

			var search = view.DeepChildren().OfType<Control>().First(e => e.Name == "QuickSearch");
			var text = search.DeepChildren().OfType<TextBox>().FirstOrDefault();

			Assert.That(text, Is.Not.Null);
		}
	}
}