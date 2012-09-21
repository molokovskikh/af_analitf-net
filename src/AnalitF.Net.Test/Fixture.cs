using System.Windows.Documents;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using NUnit.Framework;

namespace AnalitF.Net.Test
{
	[TestFixture]
	public class Fixture
	{
		[SetUp]
		public void Setup()
		{
			new Client.Config.Initializers.NHibernate().Init();
		}

		[Test, RequiresSTA]
		public void Test()
		{
			var view = new ShellView();
		}

		[Test, RequiresSTA]
		public void Show_catalog_view()
		{
			var view = new CatalogViewModel();
		}

		[Test]
		public void Init_nhibernate()
		{
			new Client.Config.Initializers.NHibernate().Init();
		}
	}
}