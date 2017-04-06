using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class ReportEditor : IEditor
	{
		private Batch batch;

		public ReportEditor(Batch batch)
		{
			this.batch = batch;
		}

		public void Updated()
		{
			if (batch.CurrentReportLine.Value?.BatchLine == null)
				return;
			if (batch.CurrentReportLine.Value.Value == 0)
				batch.Delete();
		}

		public void Committed()
		{
		}
	}

	public enum BatchMode
	{
		Normal,
		SaveUnordered,
		ReloadUnordered,
	}

	[DataContract]
	public class Batch : BaseOfferViewModel, IPrintable
	{
		private string lastUsedDir;

		public Batch()
		{
			NavigateOnShowCatalog = true;
			DisplayName = "АвтоЗаказ";
			AddressSelector = new AddressSelector(this);
			Filter = new[] {
				"Все",
				"Заказано",
				"   Минимальные",
				"   Не минимальные",
				"   Присутствующие в замороженных заказах",
				"   Ограничен лимитом",
				"Не заказано",
				"   Нет предложений",
				"   Нулевое количество",
				"   Прочее",
				"   Не сопоставлено",
				"   Лимит исчерпан"
			};
			CurrentFilter = new NotifyValue<string>("Все");
			SearchBehavior = new SearchBehavior(this);
			Lines = new NotifyValue<ObservableCollection<BatchLineView>>(new ObservableCollection<BatchLineView>());
			ReportLines = new NotifyValue<ObservableCollection<BatchLineView>>(() => {
				var query = Lines.Value.Where(l => l.Product.CultureContains(SearchBehavior.ActiveSearchTerm.Value)
					&& (l.OrderLine != null || !l.BatchLine.Status.HasFlag(ItemToOrderStatus.Ordered)));
				if (CurrentFilter.Value == Filter[1]) {
					query = query.Where(l => !l.IsNotOrdered);
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
					query = query.Where(l => l.IsSplitByLimit);
				}
				else if (CurrentFilter.Value == Filter[6]) {
					query = query.Where(l => l.IsNotOrdered);
				}
				else if (CurrentFilter.Value == Filter[7]) {
					query = query.Where(l => l.IsNotOrdered && !l.BatchLine.Status.HasFlag(ItemToOrderStatus.OffersExists));
				}
				else if (CurrentFilter.Value == Filter[8]) {
					query = query.Where(l => l.IsNotOrdered && l.BatchLine.Quantity == 0);
				}
				else if (CurrentFilter.Value == Filter[9]) {
					query = query.Where(l => l.IsNotOrdered && l.BatchLine.Quantity > 0
						&& l.BatchLine.ProductId != null
						&& l.BatchLine.Status.HasFlag(ItemToOrderStatus.OffersExists));
				}
				else if (CurrentFilter.Value == Filter[10]) {
					query = query.Where(l => l.IsNotOrdered && l.BatchLine.ProductId == null);
				}
				else if (CurrentFilter.Value == Filter[11]) {
					query = query.Where(l => l.IsLimited);
				}
				return query.OrderBy(l => l.Product).ToObservableCollection();
			}, CurrentFilter, SearchBehavior.ActiveSearchTerm);
			CurrentReportLine = new NotifyValue<BatchLineView>();
			CanDelete = CurrentReportLine.Select(l => l != null).ToValue();
			SelectedReportLines = new List<BatchLineView>();
			CanClear = Lines.CollectionChanged()
				.Select(e => e.Sender as ObservableCollection<BatchLineView>)
				.Select(v => v != null && v.Count > 0).ToValue();
			CanReload = Lines.CollectionChanged()
				.Select(e => e.Sender as ObservableCollection<BatchLineView>)
				.Select(v => CanUpload && v != null && v.Count > 0).ToValue();
			WatchForUpdate(CurrentReportLine.Select(l => l?.BatchLine));
			ActivePrint = new NotifyValue<string>();
			ActivePrint.Subscribe(ExcelExporter.ActiveProperty);
			CurrentFilter.Subscribe(_ => SearchBehavior.ActiveSearchTerm.Value = "");
			ReportEditor = new ReportEditor(this);

			PrintMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		[DataMember]
		public uint LastSelectedLine { get; set; }

		public NotifyValue<string> ActivePrint { get; set; }

		public NotifyValue<bool> CanReload { get; set; }

		public string[] Filter { get; set; }

		public NotifyValue<bool> CanClear { get; set; }

		public NotifyValue<ObservableCollection<BatchLineView>> Lines { get; set; }

		public NotifyValue<string> CurrentFilter { get; set; }

		public List<BatchLineView> SelectedReportLines { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<BatchLineView>> ReportLines { get; set; }

		public NotifyValue<BatchLineView> CurrentReportLine { get; set; }

		public AddressSelector AddressSelector { get; set; }

		public SearchBehavior SearchBehavior { get; set; }

		public NotifyValue<bool> CanDelete { get; set; }

		public bool CanUpload => Address != null;

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
						if (!User.CanPrint<Batch, BatchLine>() || Address == null)
							continue;
						var items = GetItemsFromView<BatchLineView>("ReportLines") ?? ReportLines.Value;
						docs.Add(new BatchReport(items, Address));
					}
					if ((string)item.Header == "Сводный прайс-лист") {
						if (!User.CanPrint<Batch, Offer>() || CurrentCatalog.Value == null)
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
			if (!User.CanPrint<Batch, BatchLine>() || Address == null)
				return null;
			var items = GetItemsFromView<BatchLineView>("ReportLines") ?? ReportLines.Value;
			return Preview(DisplayName, new BatchReport(items, Address));
		}

		public IEnumerable<IResult> PrintPreviewCatalogOffer()
		{
			if (!User.CanPrint<Batch, Offer>() || CurrentCatalog.Value == null)
				return null;
			var items = GetPrintableOffers();
			return Preview("Сводный прайс-лист", new CatalogOfferDocument(CurrentCatalog.Value.Name.Name, items));
		}

		public IEditor ReportEditor { get; set; }

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var grid = (DataGrid)((FrameworkElement)view).FindName("ReportLines");
			if (grid == null)
				return;
			BuildServiceColumns(Lines.Value.Select(l => l.BatchLine).FirstOrDefault(x => x != null), grid);
		}

		public static void BuildServiceColumns(BatchLine line, DataGrid grid)
		{
			if (line == null)
				return;
			grid.HorizontalScrollBarVisibility =  ScrollBarVisibility.Auto;
			foreach (var pair in line.ParsedServiceFields.Where(k => k.Key != "ReportData")) {
				var key = pair.Key;
				//todo - хорошо бы вычислять ширину колонок, но непонятно как
				//если задать с помощью звезды колонка будет зажата в минимальный размер
				//тк все имеющееся пространство уже будет распределено между фиксированными колонками
				//var width = new DataGridLength(1, DataGridLengthUnitType.Star);
				var width = new DataGridLength(50, DataGridLengthUnitType.Pixel);
				grid.Columns.Add(new DataGridTextColumn {
					Width = width,
					Header = key,
					Binding = new Binding(".") {
						Converter = new LambdaConverter<BatchLine>(l => l?.ParsedServiceFields.GetValueOrDefault(key),
							o => {
								return (o as BatchLineView).BatchLine;
							})
					}
				});
			}
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			lastUsedDir = (string)Shell.PersistentContext.GetValueOrDefault("BatchDir", Settings.Value.GetVarRoot());
			CurrentReportLine
				.Subscribe(_ => {
					CurrentElementAddress = Addresses.FirstOrDefault(a => a.Id == CurrentReportLine.Value?.Address.Id);
				});

			CurrentReportLine
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Merge(DbReloadToken)
				.SelectMany(x => Env.RxQuery(s => {
					if (CurrentReportLine.Value?.ProductId == null) {
						return new List<Offer>();
					}

					var productId = CurrentReportLine.Value.ProductId;
					return s.Query<Offer>()
						.Fetch(o => o.Price)
						.Where(o => o.ProductId == productId)
						.OrderBy(o => o.Cost)
						.ToList()
						.OrderBy(o => o.ResultCost)
						.ToList();
				})).Subscribe(UpdateOffers, CloseCancellation.Token);

			AddressSelector.Init();
			AddressSelector.FilterChanged
				.Merge(DbReloadToken)
				.SelectMany(_ => Env.RxQuery(s => {
					var ids = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
					return s.Query<BatchLine>()
						.Fetch(l => l.Address)
						.Where(l => ids.Contains(l.Address.Id))
						.ToList();
				})).CatchSubscribe(BuildLineViews, CloseCancellation);

			if (LastSelectedLine > 0)
				CurrentReportLine.Value = CurrentReportLine.Value
					?? ReportLines.Value.FirstOrDefault(v => v.BatchLine?.Id == LastSelectedLine);

			if (Address != null)
				Bus.RegisterMessageSource(Address.StatSubject);

			CurrentReportLine
				.Throttle(Consts.ScrollLoadTimeout, Env.Scheduler)
				.SelectMany(x => Env.RxQuery(s => {
					if (x?.CatalogId == null)
						return null;
					var catalogId = x.CatalogId;
					return s.Query<Catalog>()
						.Fetch(c => c.Name)
						.ThenFetch(n => n.Mnn)
						.First(c => c.Id == catalogId);
				}))
				.Subscribe(CurrentCatalog, CloseCancellation.Token);

			ReportLines
				.Select(v => v.Changed())
				.Switch()
				.Where(e => e.EventArgs.Action == NotifyCollectionChangedAction.Remove)
				.Select(x => x.EventArgs.OldItems.Cast<BatchLineView>().Select(b => b.BatchLine).Where(y => y != null).ToArray())
				.Where(x => x.Length > 0)
				.SelectMany(x => Observable.FromAsync(() => Env.Query(s => s.DeleteEach(x))))
				.CatchSubscribe(_ => {});
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.OnDeactivate();
			if (close) {
				LastSelectedLine = CurrentReportLine.Value?.BatchLine?.Id ?? 0;
				if (Settings.Value.GetVarRoot() != lastUsedDir)
					Shell.PersistentContext["BatchDir"] = lastUsedDir;
			}
			base.OnDeactivate(close);
		}

		public void Clear()
		{
			if (!CanClear)
				return;
			if (!Confirm("Удалить результат автозаказа?"))
				return;

			foreach (var line in Lines.Value)
				DeleteBatchLine(line);
		}

		public void Delete()
		{
			if (!CanDelete)
				return;
			if (!Confirm("Удалить позицию?"))
				return;

			foreach (var reportLine in SelectedReportLines.ToArray())
				DeleteBatchLine(reportLine);
		}

		private void DeleteBatchLine(BatchLineView reportLine)
		{
			Lines.Value.Remove(reportLine);
			ReportLines.Value.Remove(reportLine);
			reportLine.OrderLine?.Order.Address.RemoveLine(reportLine.OrderLine);
		}

		public void BuildLineViews(List<BatchLine> batchLines)
		{
			var orderLines = Addresses.SelectMany(a => a.ActiveOrders()).SelectMany(o => o.Lines);
			var lookup = orderLines.ToLookup(l => l.ExportBatchLineId);
			var items = batchLines.Select(l => new BatchLineView(l, lookup[l.ExportId].FirstOrDefault())).ToArray();
			Lines.Value = orderLines.Except(items.Where(x => x.OrderLine != null).Select(x => x.OrderLine))
				.Select(x => new BatchLineView(x)).Concat(items).ToObservableCollection();
			BatchLine.CalculateStyle(Address, Addresses, Lines.Value);
			Lines.Value.Where(l => l.OrderLine != null).Each(l => l.OrderLine.Configure(User));
			ReportLines.Recalculate();
		}

		public void Defectus()
		{
			Shell.NavigateRoot(new Inventory.Defectus());
		}

		public IEnumerable<IResult> Reload()
		{
			return Shell.Batch();
		}

		public IEnumerable<IResult> ReloadUnordered()
		{
			return Shell.Batch(mode: BatchMode.ReloadUnordered);
		}

		public IEnumerable<IResult> UploadAndSaveUnordered()
		{
			return InnerUpload(BatchMode.SaveUnordered);
		}

		public IEnumerable<IResult> Upload()
		{
			return InnerUpload(BatchMode.Normal);
		}

		private IEnumerable<IResult> InnerUpload(BatchMode mode)
		{
			if (!CanUpload)
				yield break;

			var haveOrders = Address.ActiveOrders().Any();
			if (haveOrders && !Confirm("После успешной отправки дефектуры будут заморожены текущие заказы.\r\n" +
				"Продолжить?"))
				yield break;

			var dialog = new OpenFileResult();
			//если установить директорию на не существующем диске диалог не будет отображен
			if (Directory.Exists(lastUsedDir))
				dialog.Dialog.InitialDirectory = lastUsedDir;
			yield return dialog;
			lastUsedDir = Path.GetDirectoryName(dialog.Dialog.FileName) ?? lastUsedDir;
			foreach (var result in Shell.Batch(dialog.Dialog.FileName, mode)) {
				yield return result;
			}
		}

		public IEnumerable<IResult> Save()
		{
			var result = new SaveFileResult(new[] {
				Tuple.Create("Отчет (*.dbf)", ".dbf"),
				Tuple.Create("Excel (*.xls)", ".xls"),
				Tuple.Create("Расширенный Excel (*.xls)", ".xls"),
				Tuple.Create("Excel (*.scv)", ".csv"),
				Tuple.Create("Здоровые люди (*.scv)", ".csv"),
			});
			var lines = Lines.Value.Where(x => x.BatchLine != null);
			yield return result;
			if (result.Dialog.FilterIndex == 1) {
				using(var writer = result.Writer()) {
					var table = new DataTable();
					var column = table.Columns.Add("KOD");
					column.ExtendedProperties.Add("scale", (byte)9);

					column = table.Columns.Add("NAME");
					column.ExtendedProperties.Add("scale", (byte)100);

					column = table.Columns.Add("KOL", typeof(double));
					column.ExtendedProperties.Add("presision", 17);
					column.ExtendedProperties.Add("scale", 3);

					column = table.Columns.Add("PRICE", typeof(double));
					column.ExtendedProperties.Add("presision", 17);
					column.ExtendedProperties.Add("scale", 3);

					column = table.Columns.Add("NOM_ZAK");
					column.ExtendedProperties.Add("scale", (byte)10);

					column = table.Columns.Add("NOM_AU");
					column.ExtendedProperties.Add("scale", (byte)6);

					var goodLines = lines.Where(l => l.OrderLine != null);
					foreach (var line in goodLines) {
						var parsedServiceFields = line.BatchLine.ParsedServiceFields.Select(f => f.Value).FirstOrDefault();
						table.Rows.Add(
							line.OrderLine.Code,
							line.OrderLine.ProductSynonym,
							line.OrderLine.Count,
							line.OrderLine.ResultCost,
							line.OrderLine.Id,
							parsedServiceFields);
					}
					Dbf2.Save(table, writer);
				}
			}
			else if (result.Dialog.FilterIndex == 2 || result.Dialog.FilterIndex == 3) {
				var exportServiceFields = result.Dialog.FilterIndex == 3;
				using(var writer = result.Writer()) {
					ExportExcel(writer.BaseStream, lines, exportServiceFields);
				}
			}
			else if (result.Dialog.FilterIndex == 4) {
				using(var writer = result.Writer()) {
					ExportCsv(writer, lines);
				}
			}
			else {
				using(var writer = result.Writer()) {
					writer.WriteLine("Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество");
					foreach (var line in ReportLines.Value.Where(x => x.BatchLine != null)) {
						var reportLine = line.BatchLine.ParsedServiceFields.Where(f => f.Key == "ReportData")
							.Select(f => f.Value)
							.FirstOrDefault();
						writer.WriteLine(reportLine);
					}
				}
			}
		}

		public void ExportCsv(TextWriter writer, IEnumerable<BatchLineView> lines)
		{
			const string quote = "\"";
			const string endQuote = "\";";

			writer.Write("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий");
			var firstLine = lines.FirstOrDefault();
			if (firstLine != null) {
				foreach (var field in firstLine.BatchLine.ParsedServiceFields) {
					writer.Write(";");
					writer.Write(field.Key);
				}
			}
			writer.WriteLine();
			foreach (var line in lines) {
				writer.Write(quote);
				writer.Write(line.Product);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Producer);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.OrderLine?.Order.PriceName);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.OrderLine?.MixedCost);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Count);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.OrderLine?.MixedSum);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Comment);
				writer.Write(quote);
				foreach (var field in line.BatchLine.ParsedServiceFields) {
					writer.Write(";\"");
					writer.Write(field.Value);
					writer.Write(quote);
				}

				writer.WriteLine();
			}
		}

		public void ExportExcel(Stream stream, IEnumerable<BatchLineView> lines, bool exportServiceFields)
		{
			var columns = new[] {
				"Наименование",
				"Производитель",
				"Прайс-лист",
				"Цена",
				"Заказ",
				"Сумма",
				"Комментарий"
			};
			if (exportServiceFields) {
				var firstLine = lines.FirstOrDefault();
				if (firstLine != null)
					columns = columns.Concat(firstLine.BatchLine.ParsedServiceFields.Select(f => f.Key)).ToArray();
			}
			var rows = lines.Select(l => new object[] {
				l.Product,
				l.Producer,
				l.OrderLine?.Order.PriceName,
				l.OrderLine?.MixedCost,
				l.Count,
				l.OrderLine?.MixedSum,
				l.Comment
			}.Concat(exportServiceFields ? l.BatchLine.ParsedServiceFields.Select(f => f.Value) : Enumerable.Empty<object>()).ToArray());
			var book = ExcelExporter.ExportTable(columns, rows);
			book.Write(stream);
		}

		public void ActivatePrint(string name)
		{
			ActivePrint.Value = name;
		}

		public void EnterReportLine()
		{
			ShowCatalog();
		}

		public override void OfferUpdated()
		{
			var posibleDeletedLine = CurrentOffer.Value?.OrderLine;
			base.OfferUpdated();

			if (posibleDeletedLine == null || CurrentOffer.Value.OrderLine != null)
				return;

			var report = Lines.Value.FirstOrDefault(x => x.OrderLine == posibleDeletedLine);
			Util.Assert(report != null, "Строка должна всегда присутствовать");
			if (report == null)
				return;
			Lines.Value.Remove(report);
			ReportLines.Value.Remove(report);
		}

		public override void OfferCommitted()
		{
			base.OfferCommitted();

			var line = LastEditOffer.Value?.OrderLine;
			if (line == null)
				return;

			if (Lines.Value.Any(x => x.OrderLine == line))
				return;

			var report = new BatchLineView(line);
			Lines.Value.Add(report);
			ReportLines.Value.Add(report);
		}
	}
}