using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Extentions;
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
using ReactiveUI;
using Remotion.Linq.Parsing;
using Order = NHibernate.Criterion.Order;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class Batch : BaseOfferViewModel, IPrintable
	{
		public List<BatchLine> Lines = new List<BatchLine>();
		private string activePrint;
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
				"Не заказано",
				"   Нет предложений",
				"   Нулевое количество",
				"   Прочее",
				"   Не сопоставлено"
			};
			CurrentFilter = new NotifyValue<string>("Все");
			SearchBehavior = new SearchBehavior(this, callUpdate: false);
			ReportLines = new NotifyValue<ObservableCollection<BatchLine>>(() => {
				var query = Lines.Where(l => l.MixedProduct.CultureContains(SearchBehavior.ActiveSearchTerm.Value)
					&& (l.Line != null || !l.Status.HasFlag(ItemToOrderStatus.Ordered)));
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
					query = query.Where(l => l.IsNotOrdered);
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
				return query.ToObservableCollection();
			}, CurrentFilter, SearchBehavior.ActiveSearchTerm);
			CurrentReportLine = new NotifyValue<BatchLine>();
			CanDelete = new NotifyValue<bool>(() => CurrentReportLine.Value != null, CurrentReportLine);
		}

		public string[] Filter { get; set; }

		public NotifyValue<string> CurrentFilter { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<BatchLine>> ReportLines { get; set; }

		public NotifyValue<BatchLine> CurrentReportLine { get; set; }

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
				return activePrint.Match("Offers")
					? User.CanPrint<Batch, Offer>()
					: User.CanPrint<Batch, BatchLine>();
			}
		}

		public override bool CanExport
		{
			get
			{
				return activePrint.Match("Offers")
					? User.CanExport<Batch, Offer>()
					: User.CanExport<Batch, BatchLine>();
			}
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var grid = (DataGrid)((FrameworkElement)view).FindName("ReportLines");
			if (grid == null)
				return;
			var line = Lines.FirstOrDefault();
			if (line == null)
				return;
			foreach (var pair in line.ParsedServiceFields) {
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

			lastUsedDir = Settings.Value.GetVarRoot();
			CurrentReportLine.Changed().Throttle(Consts.ScrollLoadTimeout, UiScheduler).Subscribe(_ => {
				if (CurrentReportLine.Value != null)
					CurrentElementAddress = Addresses.FirstOrDefault(a => a.Id == CurrentReportLine.Value.Address.Id);
				else
					CurrentElementAddress = null;
				Update();
			});

			AddressSelector.Init();
			AddressSelector.FilterChanged.Subscribe(_ => LoadLines(), CloseCancellation.Token);
			LoadLines();

			if (Address != null)
				Bus.RegisterMessageSource(Address.StatSubject);

			if (StatelessSession != null) {
				CurrentReportLine
					.Changed()
					.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
					.Subscribe(_ => {
						var line = CurrentReportLine.Value;
						if (line == null) {
							CurrentCatalog = null;
							return;
						}
						var catalogId = line.CatalogId;
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
					.Select(v => v == null
						? Observable.Empty<EventPattern<NotifyCollectionChangedEventArgs>>()
						: v.Changed())
					.Switch()
					.Where(e => e.EventArgs.Action == NotifyCollectionChangedAction.Remove)
					.Subscribe(e => StatelessSession.DeleteEach(e.EventArgs.OldItems), CloseCancellation.Token);
			}
		}

		protected override void RecreateSession()
		{
			base.RecreateSession();

			foreach (var item in AddressSelector.Addresses)
				item.Item = Session.Load<Address>(item.Item.Id);

			CalculateStatus();
			ReportLines.Recalculate();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (!Confirm("Удалить позицию?"))
				return;

			var line = CurrentReportLine.Value;
			Lines.Remove(line);
			ReportLines.Value.Remove(line);
			if (line.Line != null) {
				Address.RemoveLine(line.Line);
			}
		}

		private void LoadLines()
		{
			if (StatelessSession != null) {
				var ids = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
				Lines = StatelessSession.Query<BatchLine>()
					.Fetch(l => l.Address)
					.Where(l => ids.Contains(l.Address.Id))
					.OrderBy(l => l.ProductSynonym)
					.ToList();
			}
			CalculateStatus();
			ReportLines.Recalculate();
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
			foreach (var result in Shell.Batch(dialog.Dialog.FileName)) {
				yield return result;
			}
		}

		public IEnumerable<IResult> Save()
		{
			var dialog = new SaveFileResult(new[] {
				Tuple.Create("Отчет", ".dbf"),
				Tuple.Create("Excel", ".xls"),
				Tuple.Create("Расширенный Excel", ".xls"),
				Tuple.Create("Excel", ".csv"),
			});
			yield return dialog;
			if (dialog.Dialog.FilterIndex == 1) {
				using(var writer = dialog.Writer()) {
					var table = new DataTable();
					var column = table.Columns.Add("KOD");
					column.MaxLength = 9;

					column = table.Columns.Add("NAME");
					column.MaxLength = 100;

					column = table.Columns.Add("KOL", typeof(double));
					column.ExtendedProperties.Add("presision", 17);
					column.ExtendedProperties.Add("scale", 3);
					column = table.Columns.Add("PRICE", typeof(double));
					column.ExtendedProperties.Add("presision", 17);
					column.ExtendedProperties.Add("scale", 3);

					column = table.Columns.Add("NOM_ZAK");
					column.MaxLength = 10;

					column = table.Columns.Add("NOM_AU");
					column.MaxLength = 6;

					foreach (var line in Lines.Where(l => l.Line != null)) {
						table.Rows.Add(
							line.Line.Code,
							line.Line.ProducerSynonym,
							line.Line.Count,
							line.Line.ResultCost,
							line.Line.Id,
							line.ParsedServiceFields.Select(f => f.Value).FirstOrDefault());
					}

					Dbf2.Save(table, writer);
				}
			}
			else if (dialog.Dialog.FilterIndex == 2 || dialog.Dialog.FilterIndex == 3) {
				var exportServiceFields = dialog.Dialog.FilterIndex == 3;
				using(var writer = dialog.Writer()) {
					ExportExcel(writer.BaseStream, Lines, exportServiceFields);
				}
			}
			else {
				using(var writer = dialog.Writer()) {
					ExportCsv(writer);
				}
			}
		}

		public void ExportCsv(TextWriter writer)
		{
			const string quote = "\"";
			const string endQuote = "\";";

			writer.Write("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий");
			var firstLine = Lines.FirstOrDefault();
			if (firstLine != null) {
				foreach (var field in firstLine.ParsedServiceFields) {
					writer.Write(";");
					writer.Write(field.Key);
				}
			}
			writer.WriteLine();
			foreach (var line in Lines) {
				writer.Write(quote);
				writer.Write(line.MixedProduct);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.MixedProducer);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.PriceName);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Line != null ? (decimal?)line.Line.MixedCost : null);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.MixedCount);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Line != null ? (decimal?)line.Line.MixedSum : null);
				writer.Write(endQuote);

				writer.Write(quote);
				writer.Write(line.Comment);
				writer.Write(quote);
				foreach (var field in line.ParsedServiceFields) {
					writer.Write(";\"");
					writer.Write(field.Value);
					writer.Write(quote);
				}

				writer.WriteLine();
			}
		}

		public void ExportExcel(Stream stream, List<BatchLine> lines, bool exportServiceFields)
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
					columns = columns.Concat(firstLine.ParsedServiceFields.Select(f => f.Key)).ToArray();
			}
			var rows = lines.Select(l => new object[] {
				l.MixedProducer,
				l.MixedProduct,
				l.PriceName,
				l.Line != null ? (decimal?)l.Line.MixedCost : null,
				l.MixedCount,
				l.Line != null ? (decimal?)l.Line.MixedSum : null,
				l.Comment
			}.Concat(exportServiceFields ? l.ParsedServiceFields.Select(f => f.Value) : Enumerable.Empty<object>()).ToArray());
			var book = ExcelExporter.ExportTable(columns, rows);
			book.Write(stream);
		}

		protected override void Query()
		{
			if (StatelessSession == null)
				return;
			if (CurrentReportLine.Value == null
				|| CurrentReportLine.Value.ProductId == null) {
				Offers.Value = new List<Offer>();
				return;
			}

			var productId = CurrentReportLine.Value.ProductId;
			var offers = StatelessSession.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.ProductId == productId)
				.OrderBy(o => o.Cost)
				.ToList();
			Offers.Value = offers.OrderBy(o => o.ResultCost).ToList();
		}

		public void CalculateStatus()
		{
			if (Address == null)
				return;

			Address.ActiveOrders().SelectMany(o => o.Lines).Each(l => l.Configure(User));
			var activeLines = Addresses.SelectMany(a => a.ActiveOrders()).SelectMany(l => l.Lines)
				.Where(l => l.ExportId != null)
				.ToLookup(l => Tuple.Create(l.Order.Address.Id, l.ExportId.GetValueOrDefault()), l => l.ExportId != null ? l : null);
			var productids = Addresses.SelectMany(a => a.Orders).Where(o => o.Frozen)
				.SelectMany(o => o.Lines)
				.ToLookup(l => Tuple.Create(l.Order.Address.Id, l.ProductId));
			foreach (var line in Lines) {
				line.ExistsInFreezed = productids[Tuple.Create(line.Address.Id, line.ProductId.GetValueOrDefault())].FirstOrDefault() != null;
				line.Line = activeLines[Tuple.Create(line.Address.Id, line.ExportLineId.GetValueOrDefault())].FirstOrDefault();
			}
		}

		public PrintResult Print()
		{
			if (activePrint.Match("Offers")) {
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
			activePrint = name;
			NotifyOfPropertyChange("CanExport");
			NotifyOfPropertyChange("CanPrint");
		}

		public void EnterReportLine()
		{
			ShowCatalog();
		}

		public override void OfferUpdated()
		{
			base.OfferUpdated();

			var batchLine = CurrentReportLine.Value;
			if (batchLine == null)
				return;

			var orderLine = batchLine.Line;
			if (orderLine != null
				&& !orderLine.Order.Lines.Contains(orderLine)) {
				Lines.Remove(batchLine);
				ReportLines.Value.Remove(batchLine);
				batchLine.Line = null;
			}
		}
	}
}