using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Client.Views.Parts;
using Caliburn.Micro;
using Common.Tools.Calendar;
using Devart.Common;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class CatalogOfferViewFixture : BaseViewFixture
	{
		[Test]
		public void Open_shell()
		{
			var view = new ShellView();
			((IViewAware)shell).AttachView(new CatalogOfferView());
			ViewModelBinder.Bind(shell, view, null);
		}

		[Test]
		public void Check_view()
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = new CatalogOfferViewModel(catalog);
			var view = Bind(model);

			model.OrderWarning.OrderWarning = "test";

			var item = view.Descendants<ContentControl>().First(c => c.Name == "OrderWarning");
			Assert.That(item.Content, Is.InstanceOf<InlineEditWarningView>());
			Assert.That(XamlExtentions.AsText(item), Is.EqualTo("test"));
		}
	}
}