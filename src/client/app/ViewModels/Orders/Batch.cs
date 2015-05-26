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
	[DataContract]
	public class Batch : BaseOfferViewModel, IPrintable
	{
		private string lastUsedDir;

		public Batch()
		{
			NavigateOnShowCatalog = true;
			DisplayName = "АвтоЗаказ";
			AddressSelector = new AddressSelector(Session, this);
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
			WatchForUpdate(CurrentReportLine.Select(l => l == null ? null : l.BatchLine).ToValue());
			ActivePrint = new NotifyValue<string>();
			ActivePrint.Subscribe(ExcelExporter.ActiveProperty);
			CurrentFilter.Subscribe(_ => SearchBehavior.ActiveSearchTerm.Value = "");
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

		public bool CanUpload
		{
			get { return Address != null; }
		}

		public bool CanPrint
		{
			get
			{
				return ActivePrint.Value.Match("Offers")
					? User.CanPrint<Batch, Offer>()
					: User.CanPrint<Batch, BatchLine>();
			}
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var grid = (DataGrid)((FrameworkElement)view).FindName("ReportLines");
			if (grid == null)
				return;
			BuildServiceColumns(Lines.Value.Select(l => l.BatchLine).FirstOrDefault(), grid);
		}

		public static void BuildServiceColumns(BatchLine line, DataGrid grid)
		{
			if (line == null)
				return;
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
						Converter = new LambdaConverter<BatchLine>(l => l.ParsedServiceFields.GetValueOrDefault(key))
					}
				});
			}
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			lastUsedDir = (string)Shell.PersistentContext.GetValueOrDefault("BatchDir", Settings.Value.GetVarRoot());
			CurrentReportLine.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Subscribe(_ => {
					if (CurrentReportLine.Value != null)
						CurrentElementAddress = Addresses.FirstOrDefault(a => a.Id == CurrentReportLine.Value.BatchLine.Address.Id);
					else
						CurrentElementAddress = null;
					Update();
				});

			AddressSelector.Init();
			AddressSelector.FilterChanged.Subscribe(_ => LoadLines(), CloseCancellation.Token);
			LoadLines();
			if (LastSelectedLine > 0)
				CurrentReportLine.Value = CurrentReportLine.Value
					?? ReportLines.Value.FirstOrDefault(v => v.BatchLine.Id == LastSelectedLine);

			if (Address != null)
				Bus.RegisterMessageSource(Address.StatSubject);

			if (StatelessSession != null) {
				CurrentReportLine
					.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
					.Subscribe(_ => {
						var line = CurrentReportLine.Value;
						if (line == null) {
							CurrentCatalog = null;
							return;
						}
						var catalogId = line.BatchLine.CatalogId;
						if (catalogId != null) {
							CurrentCatalog = StatelessSession.Query<Catalog>()
								.Fetch(c => c.Name)
								.ThenFetch(n => n.Mnn)
								.First(c => c.Id == catalogId);
						}
						else {
							CurrentCatalog = null;
						}
					}, CloseCancellation.Token);

				ReportLines
					.Select(v => v.Changed())
					.Switch()
					.Where(e => e.EventArgs.Action == NotifyCollectionChangedAction.Remove)
					.CatchSubscribe(e => StatelessSession.DeleteEach(e.EventArgs.OldItems.Cast<BatchLineView>().Select(b => b.BatchLine)),
						CloseCancellation);
			}
		}

		protected override void OnDeactivate(bool close)
		{
			AddressSelector.Deinit();
			if (close) {
				LastSelectedLine = CurrentReportLine.Value != null ? CurrentReportLine.Value.BatchLine.Id : 0;
				if (Settings.Value.GetVarRoot() != lastUsedDir)
					Shell.PersistentContext["BatchDir"] = lastUsedDir;
			}
			base.OnDeactivate(close);
		}

		protected override void RecreateSession()
		{
			base.RecreateSession();

			LoadLines();
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
			if (reportLine.OrderLine != null) {
				reportLine.OrderLine.Order.Address.RemoveLine(reportLine.OrderLine);
			}
		}

		private void LoadLines()
		{
			if (StatelessSession == null)
				return;

			var ids = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
			var batchLines = StatelessSession.Query<BatchLine>()
				.Fetch(l => l.Address)
				.Where(l => ids.Contains(l.Address.Id))
				.ToList();
			BuildLineViews(batchLines);
		}

		public void BuildLineViews(List<BatchLine> batchLines)
		{
			var lookup = Addresses.SelectMany(a => a.ActiveOrders()).SelectMany(o => o.Lines)
				.ToLookup(l => l.ExportBatchLineId);
			Lines.Value = batchLines.Select(l => new BatchLineView(l, lookup[l.ExportId].FirstOrDefault())).ToObservableCollection();
			BatchLine.CalculateStyle(Address, Addresses, Lines.Value);
			Lines.Value.Where(l => l.OrderLine != null).Each(l => l.OrderLine.Configure(User));
			ReportLines.Recalculate();
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

		public IEnumerable<IResult> Save()
		{
			var result = new SaveFileResult(new[] {
				Tuple.Create("Отчет (*.dbf)", ".dbf"),
				Tuple.Create("Excel (*.xls)", ".xls"),
				Tuple.Create("Расширенный Excel (*.xls)", ".xls"),
				Tuple.Create("Excel (*.scv)", ".csv"),
				Tuple.Create("Здоровые люди (*.scv)", ".csv"),
			});

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

					var goodLines = Lines.Value.Where(l => l.OrderLine != null);
					foreach (var line in goodLines)
					{
						var parsedServiceFields = line.BatchLine.ParsedServiceFields.Select(f => f.Value).FirstOrDefault();
						table.Rows.Add(
							line.OrderLine.Code,
							line.OrderLine.ProducerSynonym,
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
					ExportExcel(writer.BaseStream, Lines.Value, exportServiceFields);
				}
			}

			else if (result.Dialog.FilterIndex == 4) {
				using(var writer = result.Writer()) {
					ExportCsv(writer);
				}
			}
			else {
				using(var writer = result.Writer()) {
					writer.WriteLine("Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество");
					foreach (var line in ReportLines.Value) {
						var reportLine = line.BatchLine.ParsedServiceFields.Where(f => f.Key == "ReportData")
							.Select(f => f.Value)
							.FirstOrDefault();
						writer.WriteLine(reportLine);
					}
				}
			}
		}

		public void ExportCsv(TextWriter writer)
		{
			const string quote = "\"";
			const string endQuote = "\";";

			writer.Write("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий");
			var firstLine = Lines.Value.FirstOrDefault();
			if (firstLine != null) {
				foreach (var field in firstLine.BatchLine.ParsedServiceFields) {
					writer.Write(";");
					writer.Write(field.Key);
				}
			}
			writer.WriteLine();
			foreach (var line in Lines.Value) {
				writer.Write(quote);
				writer.Write(line.Product);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Producer);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.OrderLine != null ? line.OrderLine.Order.PriceName : null);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.OrderLine != null ? (decimal?)line.OrderLine.MixedCost : null);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Count);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.OrderLine != null ? (decimal?)line.OrderLine.MixedSum : null);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.BatchLine.Comment);
				writer.Write(quote);
				foreach (var field in line.BatchLine.ParsedServiceFields) {
					writer.Write(";\"");
					writer.Write(field.Value);
					writer.Write(quote);
				}

				writer.WriteLine();
			}
		}

		public void ExportExcel(Stream stream, IList<BatchLineView> lines, bool exportServiceFields)
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
				l.Producer,
				l.Product,
				l.OrderLine != null ? l.OrderLine.Order.PriceName : null,
				l.OrderLine != null ? (decimal?)l.OrderLine.MixedCost : null,
				l.Count,
				l.OrderLine != null ? (decimal?)l.OrderLine.MixedSum : null,
				l.BatchLine.Comment
			}.Concat(exportServiceFields ? l.BatchLine.ParsedServiceFields.Select(f => f.Value) : Enumerable.Empty<object>()).ToArray());
			var book = ExcelExporter.ExportTable(columns, rows);
			book.Write(stream);
		}

		protected override void Query()
		{
			if (StatelessSession == null)
				return;
			if (CurrentReportLine.Value == null
				|| CurrentReportLine.Value.BatchLine.ProductId == null) {
				Offers.Value = new List<Offer>();
				return;
			}

			var productId = CurrentReportLine.Value.BatchLine.ProductId;
			var offers = StatelessSession.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.ProductId == productId)
				.OrderBy(o => o.Cost)
				.ToList();
			Offers.Value = offers.OrderBy(o => o.ResultCost).ToList();
		}

		public PrintResult Print()
		{
			if (ActivePrint.Value.Match("Offers")) {
				if (CurrentCatalog == null)
					return null;
				return new PrintResult("Сводный прайс-лист", new CatalogOfferDocument(CurrentCatalog.Name.Name, Offers.Value));
			}
			else if (Address != null) {
				return new PrintResult(DisplayName, new BatchReport(ReportLines.Value, Address));
			}
			return null;
		}

		public void ActivatePrint(string name)
		{
			ActivePrint.Value = name;
			NotifyOfPropertyChange("CanPrint");
		}

		public void EnterReportLine()
		{
			ShowCatalog();
		}

		public override void OfferUpdated()
		{
			base.OfferUpdated();

			var reportLine = CurrentReportLine.Value;
			if (reportLine == null)
				return;

			var orderLine = reportLine.OrderLine;
			if (orderLine != null
				&& !orderLine.Order.Lines.Contains(orderLine)) {
				Lines.Value.Remove(reportLine);
				ReportLines.Value.Remove(reportLine);
				reportLine.OrderLine = null;
			}
		}
	}
}