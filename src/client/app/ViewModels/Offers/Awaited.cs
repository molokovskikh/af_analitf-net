using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using Xceed.Wpf.Toolkit.Primitives;
using System.Windows.Controls;

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
			ActivePrint.Subscribe(ExcelExporter.ActiveProperty);

			PrintMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		public NotifyValue<bool> CanDelete { get; set; }
		[Export]
		public NotifyValue<ObservableCollection<AwaitedItem>> Items { get; set; }
		public NotifyValue<AwaitedItem> CurrentItem { get; set; }
		public NotifyValue<string> ActivePrint { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Env.RxQuery(s => s.Query<AwaitedItem>()
				.Fetch(i => i.Producer)
				.Fetch(i => i.Catalog)
				.ThenFetch(c => c.Name)
				.OrderBy(i => i.Catalog.Name.Name)
				.ThenBy(i => i.Catalog.Form)
				.ThenBy(i => i.Producer.Name)
				.ToObservableCollection())
				.Subscribe(x => {
					Items.Value = x;
					CurrentItem.Value = Items.Value.FirstOrDefault(i => !i.DoNotHaveOffers);
				}, CloseCancellation.Token);

			CurrentItem
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Merge(DbReloadToken)
				.Subscribe(_ => UpdateAsync(), CloseCancellation.Token);
		}

		private void UpdateAsync()
		{
			if (CurrentItem.Value == null) {
				Offers.Value = new List<Offer>();
				CurrentCatalog.Value = null;
				return;
			}

			var catalogId = CurrentItem.Value.Catalog.Id;
			Env.RxQuery(s => s.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.CatalogId == catalogId)
				.ToList()
				.OrderBy(o => o.ResultCost)
				.ToList())
				.Subscribe(UpdateOffers, CloseCancellation.Token);
		}

		public IEnumerable<IResult> Add()
		{
			var addAwaited = new AddAwaited();
			yield return new DialogResult(addAwaited);
			Items.Value.Add(addAwaited.Item);
			Items.Value = Items.Value.OrderBy(i => i.Catalog.FullName)
				.ThenBy(i => i.Producer?.Name)
				.ToObservableCollection();
			CurrentItem.Value = addAwaited.Item;
			yield return new FocusResult("Items");
		}

		public void Delete()
		{
			if (!CanDelete.Value)
				return;
			Env.Query(s => s.Delete(CurrentItem.Value)).LogResult();
			Items.Value.Remove(CurrentItem.Value);
		}

		public void SetMenuItems()
		{
			var item = new MenuItem { Header = DisplayName };
			PrintMenuItems.Add(item);

			item = new MenuItem { Header = "Сводный прайс-лист" };
			PrintMenuItems.Add(item);
		}

		public ObservableCollection<MenuItem> PrintMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }
		public bool CanPrint => true;

		public PrintResult Print()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				var printItems = PrintMenuItems.Where(i => i.IsChecked).ToList();
				if (!printItems.Any())
					printItems.Add(PrintMenuItems.First());
				foreach (var item in printItems) {
					if ((string)item.Header == DisplayName) {
						if (!User.CanPrint<Awaited, AwaitedItem>() || Address == null)
							continue;
						var items = GetItemsFromView<AwaitedItem>("Items") ?? Items.Value;
						docs.Add(new AwaitedDocument(items));
					}
					if ((string)item.Header == "Сводный прайс-лист") {
						if (!User.CanPrint<Awaited, Offer>() || CurrentCatalog.Value == null)
							continue;
						var items = GetPrintableOffers();
						docs.Add(new CatalogOfferDocument(CurrentCatalog.Value.Name.Name, items));
					}
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if (String.IsNullOrEmpty(LastOperation) || LastOperation == DisplayName)
				Coroutine.BeginExecute(PrintPreview().GetEnumerator());
			if (LastOperation == "Сводный прайс-лист")
				Coroutine.BeginExecute(PrintPreviewCatalogOffer().GetEnumerator());
			return null;
		}

		public IEnumerable<IResult> PrintPreview()
		{
			if (!User.CanPrint<Awaited, AwaitedItem>() || Address == null)
				return null;
			var items = GetItemsFromView<AwaitedItem>("Items") ?? Items.Value;
			return Preview(DisplayName, new AwaitedDocument(items));
		}

		public IEnumerable<IResult> PrintPreviewCatalogOffer()
		{
			if (!User.CanPrint<Awaited, Offer>() || CurrentCatalog.Value == null)
				return null;
			var items = GetPrintableOffers();
			return Preview("Сводный прайс-лист", new CatalogOfferDocument(CurrentCatalog.Value.Name.Name, items));
		}

		public void ActivatePrint(string name)
		{
			ActivePrint.Value = name;
		}
	}
}