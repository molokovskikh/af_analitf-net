using System;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.Views;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class WaybillDetailsFixture
	{
		[SetUp]
		public void BaseViewFixtureSetup()
		{
			ViewFixtureSetup.BindingErrors.Clear();
			ViewFixtureSetup.Setup();
		}

		[TearDown]
		public void TearDown()
		{
			if (ViewFixtureSetup.BindingErrors.Count > 0) {
				throw new Exception(ViewFixtureSetup.BindingErrors.Implode(Environment.NewLine));
			}
		}

		[Test]
		public void Show_view()
		{
			var view = new WaybillDetailsView();
		}
	}
}