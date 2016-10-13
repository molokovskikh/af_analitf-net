using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class ProductInfo : ViewAware
	{
		public static RoutedUICommand ShowCatalogCommand = new RoutedUICommand("В каталог (F2)",
			"ShowCatalog",
			typeof(ProductInfo),
			new InputGestureCollection {
				new KeyGesture(Key.F2)
			});

		public static RoutedUICommand ShowDescriptionCommand = new RoutedUICommand("Описание (F1, Пробел)",
			"ShowDescription",
			typeof(ProductInfo),
			new InputGestureCollection {
				new KeyGesture(Key.F1),
				new KeyGesture(Key.Space)
			});

		public static RoutedUICommand ShowMnnCommand = new RoutedUICommand("Показать синонимы (Ctrl+N)",
			"ShowCatalogWithMnnFilter",
			typeof(ProductInfo),
			new InputGestureCollection {
				new KeyGesture(Key.N, ModifierKeys.Control),
			});

		private WindowManager manager;
		private ShellViewModel shell;
		private BaseOffer currentOffer;
		private OfferComposedId offerId;

		public CommandBinding[] Bindings;

		public ProductInfo(BaseScreen screen, IObservable<BaseOffer> value)
		{
			CurrentCatalog = new NotifyValue<Catalog>();
			manager = screen.Manager;
			shell = screen.Shell;

			value.Subscribe(x => {
				currentOffer = x;
				offerId = (x as OrderLine)?.OfferId;
				NotifyOfPropertyChange(nameof(CurrentOffer));
			});
			value.Throttle(Consts.ScrollLoadTimeout, screen.Env.Scheduler)
				.Where(x => x?.CatalogId != CurrentCatalog.Value?.Id)
				.SelectMany(x => screen.Env.RxQuery(s => {
					if (x == null)
						return null;
					return s.Query<Catalog>()
						.Fetch(c => c.Name)
						.ThenFetch(n => n.Mnn)
						.First(c => c.Id == x.CatalogId);
				}))
				.Subscribe(CurrentCatalog);

			this.ObservableForProperty(m => m.CurrentCatalog)
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(CanShowDescription));
					NotifyOfPropertyChange(nameof(CanShowCatalog));
					NotifyOfPropertyChange(nameof(CanShowCatalogWithMnnFilter));
				});

			var binding = new CommandBinding(ShowCatalogCommand,
				(sender, args) => ShowCatalog(),
				(sender, args) => {
					args.CanExecute = CanShowCatalog;
				});
			var binding1 = new CommandBinding(ShowDescriptionCommand,
				(sender, args) => ShowDescription(),
				(sender, args) => {
					args.CanExecute = CanShowDescription;
				});
			var binding2 = new CommandBinding(ShowMnnCommand,
				(sender, args) => ShowCatalogWithMnnFilter(),
				(sender, args) => {
					args.CanExecute = CanShowCatalogWithMnnFilter;
				});
			Bindings = new[] { binding, binding1, binding2 };
		}

		public NotifyValue<Catalog> CurrentCatalog { get; set; }

		public BaseOffer CurrentOffer => currentOffer;

		public bool CanShowDescription => CurrentCatalog.Value?.Name?.Description != null;

		public bool CanShowCatalog => CurrentCatalog != null;

		public bool CanShowCatalogWithMnnFilter => CurrentCatalog.Value?.Name?.Mnn != null;

		public void ShowCatalog()
		{
			if (!CanShowCatalog)
				return;

			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog, offerId);
			shell.Navigate(offerViewModel);
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			manager.ShowDialog(new DocModel<ProductDescription>(CurrentCatalog.Value.Name.Description.Id));
		}

		public void ShowCatalogWithMnnFilter()
		{
			if (!CanShowCatalogWithMnnFilter)
				return;

			var catalogViewModel = new CatalogViewModel {
				FiltredMnn = CurrentCatalog.Value.Name.Mnn
			};
			shell.Navigate(catalogViewModel);
		}
	}
}