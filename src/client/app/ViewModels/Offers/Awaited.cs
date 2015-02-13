using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using Xceed.Wpf.Toolkit.Primitives;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class Awaited : BaseOfferViewModel, IPrintable
	{
		public Awaited()
		{
			DisplayName = "Ожидаемые позиции";
			Items = new NotifyValue<ObservableCollection<AwaitedItem>>();
			CurrentItem = new NotifyValue<AwaitedItem>();
			CanDelete = CurrentItem.Select(i => i != null).ToValue();
			ActivePrint = new NotifyValue<string>();
			ActivePrint.Subscribe(excelExporter.ActiveProperty);
		}

		public NotifyValue<bool> CanDelete { get; set; }
		[Export]
		public NotifyValue<ObservableCollection<AwaitedItem>> Items { get; set; }
		public NotifyValue<AwaitedItem> CurrentItem { get; set; }
		public NotifyValue<string> ActivePrint { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Items.Value = StatelessSession.Query<AwaitedItem>()
				.Fetch(i => i.Producer)
				.Fetch(i => i.Catalog)
				.ThenFetch(c => c.Name)
				.OrderBy(i => i.Catalog.Name.Name)
				.ThenBy(i => i.Catalog.Form)
				.ThenBy(i => i.Producer.Name)
				.ToObservableCollection();
			CurrentItem.Value = Items.Value.FirstOrDefault(i => !i.DoNotHaveOffers);

			CurrentItem
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Subscribe(_ => Query(), CloseCancellation.Token);
		}

		protected override void Query()
		{
			if (CurrentItem.Value == null) {
				Offers.Value = new List<Offer>();
				CurrentCatalog = null;
				return;
			}

			var catalogId = CurrentItem.Value.Catalog.Id;
			CurrentCatalog = StatelessSession.Query<Catalog>()
				.Fetch(c => c.Name)
				.ThenFetch(n => n.Mnn)
				.First(c => c.Id == catalogId);

			Offers.Value = StatelessSession.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.CatalogId == catalogId)
				.ToList()
				.OrderBy(o => o.ResultCost)
				.ToList();
		}

		public IEnumerable<IResult> Add()
		{
			var addAwaited = new AddAwaited();
			yield return new DialogResult(addAwaited, sizeToContent: true);
			Items.Value.Add(addAwaited.Item);
			Items.Value = Items.Value.OrderBy(i => i.Catalog.FullName)
				.ThenBy(i => i.Producer != null ? i.Producer.Name : null)
				.ToObservableCollection();
			CurrentItem.Value = addAwaited.Item;
			yield return new FocusResult("Items");
		}

		public void Delete()
		{
			if (!CanDelete.Value)
				return;
			StatelessSession.Delete(CurrentItem.Value);
			Items.Value.Remove(CurrentItem.Value);
		}

		public bool CanPrint
		{
			get
			{
				return ActivePrint.Value.Match("Offers")
					? User.CanPrint<Awaited, Offer>()
					: User.CanPrint<Awaited, AwaitedItem>();
			}
		}

		public PrintResult Print()
		{
			if (ActivePrint.Value.Match("Offers")) {
				if (CurrentCatalog == null)
					return null;
				return new PrintResult("Сводный прайс-лист", new CatalogOfferDocument(CurrentCatalog.Name.Name, Offers.Value));
			}
			else if (Address != null) {
				return new PrintResult(DisplayName, new AwaitedDocument(Items.Value));
			}
			return null;
		}

		public void ActivatePrint(string name)
		{
			ActivePrint.Value = name;
			NotifyOfPropertyChange("CanPrint");
		}
	}
}