using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class Batch2 : BaseScreen
	{
		private static string lastUsedDir;
		private Editor editor;

		public Batch2()
		{
			DisplayName = "АвтоЗаказ";
			Filter = new[] {
				"Все",
				"Заказано",
				"   Минимальные",
				"   Не минимальные",
				"   Присутствующие в замороженных заказах",
				"Не заказано",
				"   Нет предложений",
				"   Нулевое количество",
				"   Прочее",
				"   Не сопоставлено"
			};
			CurrentFilter = new NotifyValue<string>("Все");
			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			AddressSelector = new AddressSelector(Session, this);
			SearchBehavior = new SearchBehavior(this);

			OrderLines = new NotifyValue<ObservableCollection<OrderLine>>();
			CurrentOrderLine = new NotifyValue<OrderLine>();
			editor = new Editor(OrderWarning, Manager, CurrentOrderLine);
			OrderLines.Subscribe(v => editor.Lines = v);
			BatchLines = CurrentFilter.Merge(SearchBehavior.ActiveSearchTerm)
				.Select(_ => {
					var query = Lines.Value.Where(l => l.ProductSynonym.CultureContains(SearchBehavior.ActiveSearchTerm.Value));
					if (CurrentFilter.Value == Filter[1]) {
						query = query.Where(l => l.Status.HasFlag(ItemToOrderStatus.Ordered));
					}
					else if (CurrentFilter.Value == Filter[2]) {
						query = query.Where(l => !l.IsNotOrdered && l.IsMinCost);
					}
					else if (CurrentFilter.Value == Filter[3]) {
						query = query.Where(l => !l.IsNotOrdered && !l.IsMinCost);
					}
					else if (CurrentFilter.Value == Filter[4]) {
						query = query.Where(l => !l.IsNotOrdered && l.ExistsInFreezed);
					}
					else if (CurrentFilter.Value == Filter[5]) {
						query = query.Where(l => l.Status.HasFlag(ItemToOrderStatus.NotOrdered));
					}
					else if (CurrentFilter.Value == Filter[6]) {
						query = query.Where(l => l.IsNotOrdered && !l.Status.HasFlag(ItemToOrderStatus.OffersExists));
					}
					else if (CurrentFilter.Value == Filter[7]) {
						query = query.Where(l => l.IsNotOrdered && l.Quantity == 0);
					}
					else if (CurrentFilter.Value == Filter[8]) {
						query = query.Where(l => l.IsNotOrdered && l.Quantity > 0 && l.ProductId != null && l.Status.HasFlag(ItemToOrderStatus.OffersExists));
					}
					else if (CurrentFilter.Value == Filter[9]) {
						query = query.Where(l => l.IsNotOrdered && l.ProductId == null);
					}
					return query.OrderBy(l => l.ProductSynonym).ToObservableCollection();
				})
				.ToValue();
			CurrentBatchLine = new NotifyValue<BatchLine>();

			Lines = new NotifyValue<ObservableCollection<BatchLine>>(new ObservableCollection<BatchLine>());
			CanReload = Lines.CollectionChanged()
				.Select(e => e.Sender as ObservableCollection<BatchLine>)
				.Select(v => CanUpload && v != null && v.Count > 0).ToValue();
			CanDelete = CurrentBatchLine.Select(v => v != null).ToValue();
			WatchForUpdate(CurrentBatchLine);
			SelectedBatchLines = new List<BatchLine>();
			CanClear = Lines.CollectionChanged()
				.Select(e => e.Sender as ObservableCollection<BatchLine>)
				.Select(v => v != null && v.Count > 0).ToValue();
		}

		public NotifyValue<bool> CanClear { get; set; }
		public NotifyValue<ObservableCollection<BatchLine>> Lines { get; set; }
		public SearchBehavior SearchBehavior { get; set; }

		public InlineEditWarning OrderWarning { get; set; }
		public AddressSelector AddressSelector { get; set; }

		public string[] Filter { get; set; }
		public NotifyValue<string> CurrentFilter { get; set; }

		public NotifyValue<ObservableCollection<OrderLine>> OrderLines { get; set; }
		public NotifyValue<OrderLine> CurrentOrderLine { get; set; }

		public List<BatchLine> SelectedBatchLines { get; set; }
		public NotifyValue<ObservableCollection<BatchLine>> BatchLines { get; set; }
		public NotifyValue<BatchLine> CurrentBatchLine { get; set; }

		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanReload { get; set; }
		public NotifyValue<List<SentOrderLine>> HistoryOrders { get; set; }
		public ProductInfo ProductInfo { get; set; }

		public bool CanUpload
		{
			get { return Address != null; }
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(this, CurrentOrderLine);
			AddressSelector.Init();
			AddressSelector.FilterChanged.Throttle(Consts.FilterUpdateTimeout)
				.Merge(Observable.Return<object>(null))
				.Select(_ => RxQuery(s => {
					var ids = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
					return StatelessSession.Query<BatchLine>()
						.Fetch(b => b.Address)
						.Where(l => ids.Contains(l.Address.Id)).ToList();
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.CatchSubscribe(o => {
					BatchLine.CalculateStyle(Addresses, o);
					Lines.Value = new ObservableCollection<BatchLine>(o);
					BatchLines.Value = new ObservableCollection<BatchLine>(o);
					CurrentBatchLine.Value = o.FirstOrDefault();
				}, CloseCancellation);

			CurrentBatchLine.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Select(GetOrderLines)
				.CatchSubscribe(v => {
					OrderLines.Value = v;
					CurrentOrderLine.Value = v.FirstOrDefault();
				});

			HistoryOrders = CurrentOrderLine.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Select(v => v == null
					? Observable.Return(new List<SentOrderLine>())
					: RxQuery(s => {
							var productId = v.ProductId;
							return Util.Cache(cache, productId,
								k => s.Query<SentOrderLine>().Where(l => l.ProductId == productId)
									.OrderByDescending(o => o.Order.SentOn)
									.Fetch(l => l.Order)
									.ThenFetch(o => o.Price)
									.Take(20)
									.ToList());
						}))
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			AddressSelector.Deinit();
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			Attach(view, ProductInfo.Bindings);

			var grid = (DataGrid)((FrameworkElement)view).FindName("BatchLines");
			if (grid == null)
				return;
			Batch.BuildServiceColumns(Lines.Value.FirstOrDefault(), grid);
		}

		private ObservableCollection<OrderLine> GetOrderLines(BatchLine b)
		{
			return b != null
				? Address.Orders
					.SelectMany(a => a.Lines)
					.Where(l => l.ExportBatchLineId == b.ExportId)
					.OrderBy(l => l.ProductSynonym)
					.ToObservableCollection()
				: new ObservableCollection<OrderLine>();
		}

		public IEnumerable<IResult> Reload()
		{
			return Shell.Batch();
		}

		public IEnumerable<IResult> Upload()
		{
			if (!CanUpload)
				yield break;

			var haveOrders = Address.ActiveOrders().Any();
			if (haveOrders && !Confirm("После успешной отправки дефектуры будут заморожены текущие заказы.\r\n" +
				"Продолжить?"))
				yield break;

			var dialog = new OpenFileResult {
				Dialog = {
					InitialDirectory = lastUsedDir
				}
			};
			yield return dialog;
			lastUsedDir = Path.GetDirectoryName(dialog.Dialog.FileName) ?? lastUsedDir;
			foreach (var result in Shell.Batch(dialog.Dialog.FileName)) {
				yield return result;
			}
		}

		public void Clear()
		{
			if (!CanClear)
				return;
			if (!Confirm("Удалить результат автозаказа?"))
				return;

			foreach (var line in Lines.Value.ToArray())
				DeleteBatchLine(line);
		}

		public void OfferUpdated()
		{
			editor.Updated();
		}

		public void OfferCommitted()
		{
			editor.Committed();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;
			if (!Confirm("Удалить позицию?"))
				return;

			foreach (var line in SelectedBatchLines.ToArray())
				DeleteBatchLine(line);
		}

		private void DeleteBatchLine(BatchLine line)
		{
			BatchLines.Value.Remove(line);
			Lines.Value.Remove(line);
			StatelessSession.Delete(line);
			GetOrderLines(line).Each(l => l.Order.Address.RemoveLine(l));
		}
	}
}