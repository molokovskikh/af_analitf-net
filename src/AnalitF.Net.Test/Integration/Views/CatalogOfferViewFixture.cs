using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Client.Views.Parts;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Cfg;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
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

			var item = view.DeepChildren().OfType<ContentControl>().First(c => c.Name == "OrderWarning");
			Assert.That(item.Content, Is.InstanceOf<InlineEditWarningView>());
			Assert.That(AsText(item), Is.EqualTo("test"));
		}

		private static string AsText(ContentControl item)
		{
			return item.DeepChildren().Select(c => AsText(c))
				.Where(c => c != null)
				.Implode(System.Environment.NewLine);
		}

		private static string AsText(DependencyObject item)
		{
			if (item is TextBlock) {
				return ((TextBlock)item).Text;
			}

			if (item is ContentControl && ((ContentControl)item).Content is string) {
				return (string)((ContentControl)item).Content;
			}
			return null;
		}
	}
}