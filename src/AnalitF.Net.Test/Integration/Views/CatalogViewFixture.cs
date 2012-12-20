using AnalitF.Net.Client.Views;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class CatalogViewFixture : BaseViewFixture
	{
		[Test]
		public void Open_shell()
		{
			var view = new ShellView();
			((IViewAware)shell).AttachView(new CatalogOfferView());
			ViewModelBinder.Bind(shell, view, null);
		}
	}
}