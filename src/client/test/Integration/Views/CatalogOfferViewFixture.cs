using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Client.Views.Offers;
using AnalitF.Net.Client.Views.Parts;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using Devart.Common;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using WpfHelper = AnalitF.Net.Client.Helpers.WpfHelper;

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
			Assert.That(item.AsText(), Is.EqualTo("test"));
		}

		[Test]
		public void Rebuild_styles()
		{
			StyleHelper.Reset();

			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = new CatalogOfferViewModel(catalog);
			var view = Bind(model);

			StyleHelper.BuildStyles(App.Current.Resources, new[] { new CustomStyle("Junk", "Red") });
			bus.SendMessage(settings);
			testScheduler.AdvanceByMs(1000);

			var legend = view.Descendants().OfType<Panel>().First(p => p.Name == "Legend");
			var label = legend.Descendants<Label>()
				.First(l => l.Style != null && l.Style.Setters.OfType<Setter>().Any(s => s.Property == ContentControl.ContentProperty && Equals(s.Value, "Уцененные препараты")));
			var setter = label.Style.Setters.OfType<Setter>().First(s => s.Property == Control.BackgroundProperty);
			Assert.AreEqual(Colors.Red, ((SolidColorBrush)setter.Value).Color);
		}

		[Test(Description = "Часто в результате переименований view теряется, ищем подобные ошибки")]
		public void Check_view_binding()
		{
			var ignore = new [] { typeof(BaseShell), typeof(BaseScreen) };
			var viewModels = typeof(AppBootstrapper).Assembly.GetTypes()
				.Where(t => !t.IsAbstract && !t.IsInterface && typeof(IScreen).IsAssignableFrom(t) && !ignore.Contains(t));
			foreach (var type in viewModels) {
				Assert.IsNotNull(ViewLocator.LocateTypeForModelType(type, null, null), type.ToString());
			}
		}

		[Test]
		public void Disable_clipboard()
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = new CatalogOfferViewModel(catalog);
			model.User.Permissions.Remove(model.User.Permissions.FirstOrDefault(p => p.Name.Match("FPCPL")));
			var view = Bind(model);
			view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var grid = view.Descendants<DataGrid>().First(g => g.Name == "Offers");
			Assert.AreEqual(DataGridClipboardCopyMode.None, grid.ClipboardCopyMode);
		}
	}
}