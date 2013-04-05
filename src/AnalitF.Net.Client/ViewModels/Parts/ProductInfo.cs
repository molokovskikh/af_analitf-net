﻿using System;
using System.Linq;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using WindowManager = AnalitF.Net.Client.Extentions.WindowManager;

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

		private IStatelessSession StatelessSession;
		private WindowManager Manager;
		private ShellViewModel Shell;

		private BaseOffer currentOffer;
		private Catalog currentCatalog;

		private OfferComposedId offerId;

		public CommandBinding[] Bindings;

		public ProductInfo(IStatelessSession session, WindowManager manager, ShellViewModel shell)
		{
			StatelessSession = session;
			Manager = manager;
			Shell = shell;

			this.ObservableForProperty(m => m.CurrentCatalog)
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanShowDescription");
					NotifyOfPropertyChange("CanShowCatalog");
					NotifyOfPropertyChange("CanShowCatalogWithMnnFilter");
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
			Bindings = new [] { binding, binding1, binding2 };
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
		}

		public BaseOffer CurrentOffer
		{
			get { return currentOffer; }
			set
			{
				var line = value as OrderLine;
				if (line != null)
					offerId = line.OfferId;

				currentOffer = value;
				if (value == null) {
					currentCatalog = null;
				}
				else if (currentCatalog == null || currentCatalog.Id != currentOffer.CatalogId) {
					var catalogId = CurrentOffer.CatalogId;
					currentCatalog = StatelessSession.Query<Catalog>()
						.Fetch(c => c.Name)
						.ThenFetch(n => n.Mnn)
						.First(c => c.Id == catalogId);
				}
				NotifyOfPropertyChange("CurrentOffer");
				NotifyOfPropertyChange("CurrentCatalog");
			}
		}

		public bool CanShowDescription
		{
			get
			{
				return CurrentCatalog != null
					&& CurrentCatalog.Name.Description != null;
			}
		}

		public bool CanShowCatalog
		{
			get { return CurrentCatalog != null; }
		}

		public bool CanShowCatalogWithMnnFilter
		{
			get { return CurrentCatalog != null && CurrentCatalog.Name.Mnn != null; }
		}

		public void ShowCatalog()
		{
			if (!CanShowCatalog)
				return;

			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog);
			offerViewModel.CurrentOffer = StatelessSession.Get<Offer>(offerId);

			Shell.Navigate(offerViewModel);
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalog.Name.Description));
		}

		public void ShowCatalogWithMnnFilter()
		{
			if (!CanShowCatalogWithMnnFilter)
				return;

			var catalogViewModel = new CatalogViewModel {
				FiltredMnn = CurrentCatalog.Name.Mnn
			};
			Shell.Navigate(catalogViewModel);
		}
	}
}