using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Dapper;
using NHibernate.Linq;
using NPOI.SS.UserModel;
using ReactiveUI;
using HorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;
using VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment;
using System.ComponentModel.DataAnnotations;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels.Inventory;

namespace AnalitF.Net.Client.ViewModels
{
	public class EditSumSettings
	{
		[Display(Name = "Сумма с НДС")]
		public decimal UserSum { get; set; }

		[Display(Name = "Сумма НДС")]
		public decimal UserTaxSum { get; set; }

		public EditSumSettings(Waybill waybill)
		{
			UserSum = waybill.DisplayedSum;
			UserTaxSum = waybill.DisplayedTaxSum;
		}
	}

	public class WaybillDetails : BaseScreen
	{
		private uint id;
		private PriceTag priceTag;
		private PriceTag rackingMap;

		//для восстановления состояния
		public WaybillDetails(long id)
			: this((uint)id)
		{
		}

		public WaybillDetails(uint id)
		{
			DisplayName = "Детализация накладной";
			this.id = id;
			InitFields();
			CurrentWaybillLine = CurrentLine.OfType<WaybillLine>().ToValue();

			Settings.Subscribe(_ => Calculate());
			CurrentTax.Subscribe(v => {
				if (Lines.Value == null)
					return;
				if (v == null || v.Value == -1)
					Lines.Value.Filter = null;
				else
					Lines.Value.Filter = o => ((WaybillLine)o).Nds == v.Value;
			});
			Lines.Select(v => v == null
				? Observable.Empty<EventPattern<NotifyCollectionChangedEventArgs>>()
				: v.ToCollectionChanged())
			.Switch()
			.Subscribe(e => {
				if (e.EventArgs.Action == NotifyCollectionChangedAction.Remove) {
					e.EventArgs.OldItems.OfType<WaybillLine>().Each(l => Waybill.RemoveLine(l));
				}
				else if (e.EventArgs.Action == NotifyCollectionChangedAction.Add) {
					e.EventArgs.NewItems.OfType<WaybillLine>().Each(l => Waybill.AddLine(l));
				}
			});
			OrderLines = CurrentWaybillLine
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Select(v => {
					if (v == null)
						return new List<SentOrderLine>();
					var lineId = v.Id;
					var orderLineIds = Session.Query<WaybillOrder>().Where(l => l.DocumentLineId == lineId)
						.Select(l => (uint?)l.OrderLineId)
						.ToArray();
					var line = Session.Query<SentOrderLine>().FirstOrDefault(l => orderLineIds.Contains(l.ServerId));
					if (line != null) {
						CurrentOrderLine.Value = line;
						line.Order.Lines.Each(l => l.Configure(User));
						return line.Order.Lines.OrderBy(l => l.ProductSynonym).ToList();
					}
					return new List<SentOrderLine>();
				})
				.ToValue(CloseCancellation);
			EmptyLabelVisibility = OrderLines
				.Select(v => v == null || v.Count == 0 ? Visibility.Visible : Visibility.Collapsed)
				.ToValue();
			OrderDetailsVisibility = EmptyLabelVisibility
				.Select(v => v == Visibility.Collapsed ? Visibility.Visible :  Visibility.Collapsed)
				.ToValue();
		}

		public Waybill Waybill { get; set; }
		//datagrid для обозначения новой строки использует специальный внутренний объект
		//что бы избежать ошибок в тестах
		public NotifyValue<object> CurrentLine { get; set; }
		public NotifyValue<ListCollectionView> Lines { get; set; }
		public NotifyValue<WaybillLine> CurrentWaybillLine { get; set; }
		public List<ValueDescription<int?>> Taxes { get; set; }
		public NotifyValue<ValueDescription<int?>> CurrentTax { get; set; }

		public NotifyValue<List<SentOrderLine>> OrderLines { get; set; }
		public NotifyValue<SentOrderLine> CurrentOrderLine { get; set; }
		public NotifyValue<Visibility> OrderDetailsVisibility { get; set; }
		public NotifyValue<Visibility> EmptyLabelVisibility { get; set; }
		public NotifyValue<bool> IsRejectVisible { get; set; }
		public NotifyValue<Reject> Reject { get; set; }
		public NotifyValue<bool> CanStock { get; set; }
		public NotifyValue<bool> CanToEditable { get; set; }
		public NotifyValue<bool> CanEditSum { get; set; }

		private void Calculate()
		{
			//в случае если мы восстановили значение из сессии
			Waybill?.Calculate(Settings.Value, Shell?.SpecialMarkupProducts.Value);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			OnCloseDisposable.Add(Shell.SpecialMarkupProducts.Subscribe(_ => Calculate()));

			if (Session == null)
				return;

			Waybill = Session.Get<Waybill>(id);
			if (Waybill == null) {
				return;
			}
			Waybill.Settings = Settings;
			Waybill.ObservableForProperty(x => x.Status, skipInitial: false)
				.Select(x => x.Value == DocStatus.NotPosted).Subscribe(CanStock);
			Waybill.ObservableForProperty(x => x.Status, skipInitial: false)
				.Select(x => x.Value == DocStatus.NotPosted && Waybill.SupplierSum == null).Subscribe(CanEditSum);

			Waybill.ObservableForProperty(m => (object)m.Status, skipInitial: false)
				.Merge(Waybill.ObservableForProperty(m => (object)m.IsCreatedByUser))
				.Select(_ => Waybill.Status == DocStatus.NotPosted && !Waybill.IsCreatedByUser)
				.Subscribe(CanToEditable);

			if (Waybill.IsNew)
			{
				Waybill.IsNew = false;
				if (Shell.NewDocsCount.Value > 0)
					Shell.NewDocsCount.Value--;
			}

			Calculate();

			var stockids =  Waybill.Lines.Where(x => x.StockId != null).Select(x => x.StockId).ToArray();
			RxQuery(s => s.Query<Stock>().Where(x => stockids.Contains(x.ServerId)).ToDictionary(x => x.ServerId))
				.ObserveOn(UiScheduler)
				.Subscribe(x => {
					Waybill.Lines.Each(y => {
						y.Stock = x.GetValueOrDefault(y.StockId);
					});
				});

			Lines.Value = new ListCollectionView(Waybill.Lines.OrderBy(l => l.Product).ToList());
			Taxes = new List<ValueDescription<int?>> {
				new ValueDescription<int?>("Все", -1),
			};
			Taxes.AddRange(Lines.Value.Cast<WaybillLine>().Select(l => l.Nds).Distinct()
				.OrderBy(t => t)
				.Select(t => new ValueDescription<int?>(((object)t ?? "Нет значения").ToString(), t)));
			CurrentTax.Value = Taxes.FirstOrDefault();
			Reject = CurrentWaybillLine
				.Throttle(Consts.ScrollLoadTimeout, Scheduler)
				.Select(v => RxQuery(s => {
					if (v?.RejectId == null)
						return null;
					return s.Get<Reject>(v.RejectId.Value);
				}))
				.Switch()
				.ToValue(CloseCancellation);
			IsRejectVisible = Reject.Select(r => r != null).ToValue();
			if (Waybill.IsCreatedByUser)
			{
				EmptyLabelVisibility = EmptyLabelVisibility.Select(s => Visibility.Collapsed).ToValue();
				OrderDetailsVisibility = OrderDetailsVisibility.Select(s => Visibility.Collapsed).ToValue();
			}

			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.PriceTag, Waybill.Address))
				.Subscribe(x => priceTag = x);
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.RackingMap, Waybill.Address))
				.Subscribe(x => rackingMap = x);
		}

		protected override void OnActivate()
		{
			if (Waybill == null) {
				Manager.Warning("Накладная отсутствует");
				IsSuccessfulActivated = false;
				return;
			}
			base.OnActivate();
		}

		public void Tags()
		{
			var tags = PrintableLinesForTag();
			Shell.Navigate(new Tags(Waybill.Address, tags));
		}

		public IEnumerable<IResult> PrintRegistry()
		{
			var doc = new RegistryDocument(Waybill, PrintableLines());
			var docSettings = doc.Settings;
			yield return new DialogResult(new RegistryDocSettings((RegistryDocumentSettings)docSettings));
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult("Реестр", doc)), fullScreen: true);
		}

		public IEnumerable<IResult> PrintWaybill()
		{
			return Preview("Накладная", new WaybillDocument(Waybill, PrintableLines()));
		}

		public IEnumerable<IResult> PrintAct()
		{
			return Preview("Акт прихода", new WaybillActDocument(Waybill, PrintableLines()));
		}

		public IEnumerable<IResult> PrintProtocol()
		{
			return Preview("Протокол согласования цен", new WaybillProtocolDocument(Waybill, PrintableLines()));
		}

		public IEnumerable<IResult> PrintInvoice()
		{
			return Preview("Счет-фактура", new InvoiceDocument(Waybill));
		}

		private IList<WaybillLine> PrintableLines()
		{
			//в случае редактирование пользовательской накладной в коллекции будет NamedObject
			return Lines.Value.OfType<WaybillLine>().Where(l => l.Print).ToList();
		}

		public IResult PrintRackingMap()
		{
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Стеллажная карта",
				Document = new RackingMapDocument(PrintableLinesForTag(), Settings.Value, rackingMap).Build()
			}, fullScreen: true);
		}

		public IResult PrintPriceTags()
		{
			var settings = Settings.Value.PriceTags.First(x => x.Address.Id == Waybill.Address.Id);
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Ценники",
				Document = new PriceTagDocument(PrintableLinesForTag(), settings, priceTag).Build()
			}, fullScreen: true);
		}

		private List<TagPrintable> PrintableLinesForTag()
		{
			return PrintableLines().Select(x => new TagPrintable() {
				Product = x.Product,
				RetailCost = x.RetailCost,
				SupplierName = Waybill.Supplier?.FullName,
				ClientName = User?.FullName,
				Producer = x.Producer,
				ProviderDocumentId = Waybill.ProviderDocumentId,
				DocumentDate = Waybill.DocumentDate,
				Barcode = x.EAN13,
				AltBarcode = x.Stock?.AltBarcode,
				SerialNumber = x.SerialNumber,
				Quantity = x.Quantity.GetValueOrDefault(),
				Period = x.Period,
				Country = x.Country,
				Certificates = x.Certificates,
				SupplierPriceMarkup = x.SupplierPriceMarkup,
				RetailMarkup = x.RetailMarkup,
				SupplierCost = x.SupplierCost,
				Nds = x.Stock?.NdsPers ?? 0,
				Np = x.Stock?.NpPers ?? 0,
				WaybillId = Waybill.Id,
				CopyCount = x.Quantity.GetValueOrDefault(),
				Selected = true,
			}).ToList();
		}

		public IEnumerable<IResult> EditSum()
		{
			var settings = new EditSumSettings(Waybill);
			yield return new DialogResult(new SimpleSettings(settings));
			if (settings.UserSum <= 0 || settings.UserTaxSum <= 0) {
				MessageBox.Show(
						"Суммы должны быть больше нуля",
						"АналитФАРМАЦИЯ: Внимание",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				yield break;
			}

			Waybill.UserSum = settings.UserSum;
			Waybill.UserTaxSum = settings.UserTaxSum;
			Session.Update(Waybill);
			Session.Flush();
			Env.Bus.SendMessage("Changed", "db");
		}

		public IEnumerable<IResult> ConsumptionReport()
		{
			var commnand = new ConsumptionReport(Waybill);
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}

		public IResult ExportWaybill()
		{
			var columns = new[] {
				"№ пп",
				"Наименование и краткая характеристика товара",
				"",
				"Серия товара",
				"Сертификат",
				"Срок годности",
				"Производитель",
				"Цена без НДС, руб",
				"Затребован.колич.",
				"Опт. надб. %",
				"Отпуск. цена пос-ка без НДС, руб",
				"НДС пос-ка, руб",
				"Отпуск. цена пос-ка с НДС, руб",
				"Розн. торг. надб. %",
                "Розн. торг. надб. руб",
                "Розн. цена за ед., руб",
				"Кол-во",
				"Розн. сумма, руб"
			};
			var items = PrintableLines().Select((l, i) => new object[] {
				i + 1,
				l.Product,
				$"{(l.ActualVitallyImportant ? "ЖВ" : "") }",
				l.SerialNumber,
				l.Certificates,
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.Quantity,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.Nds,
				l.SupplierCost,
				l.RetailMarkup,
        l.RetailMarkupInRubles,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});

			int startTableRow = 8;
			var book = ExcelExporter.ExportTable(columns, items, startTableRow);
			var sheet = book.GetSheetAt(0);
			int[] afterItemsPosition = new int[] { items.Count() + startTableRow + 2, columns.Length };
			var styleForGrid = book.CreateCellStyle();

			styleForGrid.BorderBottom = BorderStyle.Thin;
			styleForGrid.BorderLeft = BorderStyle.Thin;
			styleForGrid.BorderRight = BorderStyle.Thin;
			styleForGrid.BorderTop = BorderStyle.Thin;
			styleForGrid.WrapText = true;
			styleForGrid.Alignment = HorizontalAlignment.Center;
			styleForGrid.VerticalAlignment = VerticalAlignment.Center;
			var maxWidthCollection = new Dictionary<int, int>();

			for (int y = startTableRow; y < afterItemsPosition[0] - 1; y++) {
				for (int x = 0; x < afterItemsPosition[1]; x++) {
					var rowI = sheet.GetRow(y);
					var cellI = rowI.GetCell(x);

					if (cellI == null) {
						cellI = rowI.CreateCell(x);
					} else {
						if(y > startTableRow)
						{
							var width = 256 * cellI.ToString().Length;
							width = width < 256*5 ? 256*5 : width;
							if(!maxWidthCollection.ContainsKey(x))
							{
								maxWidthCollection.Add(x, width);
							}
							if(width > maxWidthCollection[x])
							{
								maxWidthCollection[x] = width;
							}
							sheet.SetColumnWidth(x, maxWidthCollection[x]);
						}
					}
					cellI.CellStyle = styleForGrid;
				}
			}

			sheet.CreateRow(1).CreateCell(7).SetCellValue(
				$"Наименование организации: Сотрудник {Waybill.WaybillSettings.FullName}");

			var row = sheet.CreateRow(2);
			row.CreateCell(4).SetCellValue("Отдел:_______________________________________");

			row = sheet.CreateRow(3);
			row.CreateCell(1).SetCellValue("Требование №");
			row.CreateCell(6)
				.SetCellValue($"Накладная №{ (Waybill.ProviderDocumentId?.Length > 0 ? Waybill.ProviderDocumentId :  "_______________________")} {Waybill.SupplierName}");

			row = sheet.CreateRow(4);
			row.CreateCell(1).SetCellValue("от \"___\"_________________20___г");
			row.CreateCell(7).SetCellValue($"от {Waybill.DocumentDate.ToLongDateString()}");

			row = sheet.CreateRow(5);
			row.CreateCell(1).SetCellValue("Кому: Аптечный пункт");
			row.CreateCell(6).SetCellValue("Через кого");
			row.CreateCell(7).SetCellValue("_______________________");

			row = sheet.CreateRow(6);
			row.CreateCell(1).SetCellValue("Основание отпуска_______________________");
			row.CreateCell(6).SetCellValue("Доверенность №_____");
			row.CreateCell(7).SetCellValue("от \"___\"_________________20___г");

			NumberToRubleWords converter = new NumberToRubleWords();

			row = sheet.CreateRow(afterItemsPosition[0]);
			row.CreateCell(1).SetCellValue($"Продажная сумма {converter.Convert(Waybill.RetailSum)}");
			row.CreateCell(afterItemsPosition[1] - 1).SetCellValue(decimal.ToDouble(Waybill.RetailSum));

			row = sheet.CreateRow(afterItemsPosition[0] + 2);
			row.CreateCell(1).SetCellValue($"Сумма поставки {converter.Convert(Waybill.Sum)}");
			row.CreateCell(afterItemsPosition[1] - 1).SetCellValue(decimal.ToDouble(Waybill.Sum));

			row = sheet.CreateRow(afterItemsPosition[0] + 6);
			row.CreateCell(1).SetCellValue("Затребовал:  ");
			row.CreateCell(5).SetCellValue("Отпустил: Сдал (выдал)________________");

			row = sheet.CreateRow(afterItemsPosition[0] + 8);
			row.CreateCell(5).SetCellValue("Получил:Принял(получил)______________");

			row = sheet.CreateRow(afterItemsPosition[0] + 9);
			row.CreateCell(1).SetCellValue("место печати       подпись     _________________");

			row = sheet.CreateRow(afterItemsPosition[0] + 10);
			row.CreateCell(5).SetCellValue("Получил:Принял(получил)______________");

			row = sheet.CreateRow(afterItemsPosition[0] + 12);
			row.CreateCell(1).SetCellValue("\" ____\" _______________20__г");
			row.CreateCell(5).SetCellValue("Главный (старший)бухгалтер ________________");

			return ExcelExporter.Export(book);
		}

		public IResult RestoredExportWaybill()
		{
			var columns = new[] {
				"№ пп",
				"Наименование и краткая характеристика товара",
				"Серия товара Сертификат",
				"Срок годности",
				"Производитель",
				"Цена без НДС, руб",
				"Затребован.колич.",
				"Опт. надб. %",
				"Отпуск. цена пос-ка без НДС, руб",
				"НДС пос-ка, руб",
				"Отпуск. цена пос-ка с НДС, руб",
				"Розн. торг. надб. %",
                "Розн. торг. надб. руб",
                "Розн. цена за ед., руб",
				"Кол-во",
				"Розн. сумма, руб"
			};
			var items = PrintableLines().Select((l, i) => new object[] {
				i + 1,
				l.Product,
				$"{l.SerialNumber} {l.Certificates}",
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.Quantity,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.Nds,
				l.SupplierCost,
				l.RetailMarkup,
                l.RetailMarkupInRubles,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = ExcelExporter.ExportTable(columns, items, 8);

			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue(
				$"Наименование организации: Сотрудник {Waybill.WaybillSettings.FullName}");

			var row = sheet.CreateRow(2);
			row.CreateCell(3).SetCellValue("Отдел:");
			row.CreateCell(4).SetCellValue("_______________________________________");

			row = sheet.CreateRow(3);
			row.CreateCell(0).SetCellValue("Требование №");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("Накладная №");
			row.CreateCell(6).SetCellValue("_______________________");

			row = sheet.CreateRow(4);
			row.CreateCell(1).SetCellValue("от \"___\"_________________20___г");
			row.CreateCell(6).SetCellValue("от \"___\"_________________20___г");

			row = sheet.CreateRow(5);
			row.CreateCell(0).SetCellValue("Кому: Аптечный пункт");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("Через кого");
			row.CreateCell(6).SetCellValue("_______________________");

			row = sheet.CreateRow(6);
			row.CreateCell(0).SetCellValue("Основание отпуска");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("Доверенность №_____");
			row.CreateCell(6).SetCellValue("от \"___\"_________________20___г");

			return ExcelExporter.Export(book);
		}

		public IResult ExportRegistry()
		{
			var columns = new[] {
				"№ пп",
				"Наименование",
				"Серия товара",
				"Срок годности",
				"Производитель",
				"Цена без НДС, руб",
				"Цена ГР, руб",
				"Опт. надб. %",
				"Отпуск. цена пос-ка без НДС, руб",
				"НДС пос-ка, руб",
				"Отпуск. цена пос-ка с НДС, руб",
				"Розн. торг. надб. %",
                "Розн. торг. надб. руб",
				"Розн. цена за ед., руб",
				"Кол-во",
				"Розн. сумма, руб"
			};
			var items = PrintableLines().Select((l, i) => new object[] {
				i + 1,
				l.Product,
				l.SerialNumber,
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.RegistryCost,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.Nds,
				l.SupplierCost,
				l.RetailMarkup,
        l.RetailMarkupInRubles,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = ExcelExporter.ExportTable(columns, items, 5);
			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue("Реестр");
			sheet.CreateRow(2).CreateCell(3)
				.SetCellValue("розничных цен на лекарственные средства и изделия медицинского назначения,");
			sheet.CreateRow(3).CreateCell(3)
				.SetCellValue(
					$"полученные от {Waybill.SupplierName}-по счету (накладной) №{Waybill.ProviderDocumentId} от {Waybill.DocumentDate:d}");
			return ExcelExporter.Export(book);
		}

		public override IEnumerable<IResult> Download(Loadable loadable)
		{
			var supplier = ((WaybillLine)loadable).Waybill.SafeSupplier;
			if (supplier == null || !supplier.HaveCertificates) {
				yield return new MessageResult("Данный поставщик не предоставляет сертификаты в АналитФармация." +
					"\r\nОбратитесь к поставщику.",
					MessageResult.MessageType.Warning);
				yield break;
			}
			base.Download(loadable);
		}

		public void HideReject()
		{
			IsRejectVisible.Value = false;
		}

		public void Stock()
		{
			if (!CanStock.Value)
				return;
			Waybill.Stock(Session);
			Manager.Notify("Накладная оприходована");
		}

		public void ToEditable()
		{
			if (!CanToEditable.Value)
				return;

			if (!Confirm("Накладная будет переведена в редактируемые, продолжить?"))
				return;

			Waybill.IsCreatedByUser = true;
			Manager.Notify("Накладная переведена в редактируемые");
		}

#if DEBUG
		public override object[] GetRebuildArgs()
		{
			return new object[] {
				id
			};
		}
#endif

		public IEnumerable<IResult> AddFromCatalog()
		{
			if (!Waybill.IsCreatedByUser)
				yield break;
			var dlg = new AddWaybillLineFromCatalog();
			yield return new DialogResult(dlg);
			if (dlg.WasCancelled)
				yield break;
			var line = new WaybillLine(Waybill) {
				CatalogId = dlg.CurrentCatalog.Value.Id,
				ProductId = Session.Query<Product>().FirstOrDefault(r => r.CatalogId == dlg.CurrentCatalog.Value.Id)?.Id,
				Product = dlg.CurrentCatalog.Value.FullName,
				ProducerId = dlg.CurrentProducer.Value.Id,
				Producer = dlg.CurrentProducer.Value.Name,
				SupplierCost = dlg.SupplierCost.Value,
				Quantity = dlg.Quantity.Value
			};
			CurrentLine.Value = Lines.Value.AddNewItem(line);
			CurrentWaybillLine = CurrentLine.OfType<WaybillLine>().ToValue();
		}
	}
}