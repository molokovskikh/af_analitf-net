using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogOfferViewModel : BaseOfferViewModel, IPrintable, IExportable
	{
		private const string allRegionLabel = "Все регионы";

		private string currentRegion;
		private List<string> regions;
		private string currentFilter;
		private bool groupByProduct;

		private decimal retailMarkup;
		private List<MaxProducerCost> maxProducerCosts;
		private List<SentOrderLine> historyOrders;

		public CatalogOfferViewModel(Catalog catalog)
		{
			DisplayName = "Сводный прайс-лист";
			NeedToCalculateDiff = true;
			GroupByProduct = Settings.GroupByProduct;
			CurrentCatalog = catalog;
			Filters = new [] { "Все", "Основные", "Неосновные" };
			CurrentFilter = Filters[0];
			CurrentRegion = allRegionLabel;
			CurrentProducer = AllProducerLabel;

			this.ObservableForProperty(m => m.CurrentRegion)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Merge(this.ObservableForProperty(m => m.CurrentFilter))
				.Subscribe(e => Filter());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => NotifyOfPropertyChange("RetailMarkup"));

			this.ObservableForProperty(m => m.RetailMarkup)
				.Subscribe(_ => NotifyOfPropertyChange("RetailCost"));

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => NotifyOfPropertyChange("Price"));

			this.ObservableForProperty(m => m.CurrentOffer)
				.Where(o => o != null)
				.Throttle(TimeSpan.FromMilliseconds(2000), Scheduler)
				.Subscribe(_ => LoadHistoryOrders());

			Filter();
			UpdateMaxProducers();

			CurrentOffer = Offers.FirstOrDefault(o => o.Price.BasePrice);
			if (CurrentOffer == null)
				CurrentOffer = offers.FirstOrDefault();

			UpdateRegions();
			UpdateProducers();
		}

		//TODO: похоже что исключение не обрабатывается все падает
		public void LoadHistoryOrders()
		{
			if (CurrentOffer == null || Address == null)
				return;

			var query = Session.Query<SentOrderLine>();
			if (Settings.GroupByProduct) {
				query = query.Where(o => o.CatalogId == CurrentOffer.CatalogId);
			}
			else {
				query = query.Where(o => o.ProductId == CurrentOffer.ProductId);
			}
			HistoryOrders = query
				.Where(o => o.Order.Address == Address)
				.OrderByDescending(o => o.Order.SentOn)
				.Take(20)
				.ToList();

			var begin = DateTime.Now.AddMonths(-1);
			var values = Session.CreateSQLQuery(@"select avg(cost) as avgCost, avg(count) as avgCount
from SentOrderLines ol
join SentOrders o on o.Id = ol.SentOrderId
where o.SentOn > :begin and ol.ProductId = :productId and o.AddressId = :addressId")
				.SetParameter("begin", begin)
				.SetParameter("productId", CurrentOffer.ProductId)
				.SetParameter("addressId", Address.Id)
				.UniqueResult<object[]>();
			CurrentOffer.PrevOrderAvgCost = (decimal?)values[0];
			CurrentOffer.PrevOrderAvgCount = (decimal?)values[1];
		}

		private void UpdateMaxProducers()
		{
			if (CurrentCatalog == null)
				return;

			MaxProducerCosts = Session.Query<MaxProducerCost>()
				.Where(c => c.CatalogId == CurrentCatalog.Id)
				.OrderBy(c => c.Product)
				.ThenBy(c => c.Producer)
				.ToList();
		}

		private void UpdateRegions()
		{
			var offerRegions = Offers.Select(o => o.RegionName).Distinct().OrderBy(r => r).ToList();
			Regions = new[] { allRegionLabel }.Concat(offerRegions).ToList();
		}

		private void Filter()
		{
			var queryable = StatelessSession.Query<Offer>().Where(o => o.CatalogId == CurrentCatalog.Id);
			if (CurrentRegion != allRegionLabel) {
				queryable = queryable.Where(o => o.RegionName == CurrentRegion);
			}
			if (CurrentProducer != AllProducerLabel) {
				queryable = queryable.Where(o => o.Producer == CurrentProducer);
			}
			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(o => o.Price.BasePrice);
			}
			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(o => !o.Price.BasePrice);
			}

			var offers = queryable.Fetch(o => o.Price).ToList();
			offers = Sort(offers);
			Offers = offers;
			Calculate();
		}

		public string[] Filters { get; set; }

		public List<MaxProducerCost> MaxProducerCosts
		{
			get { return maxProducerCosts; }
			set
			{
				maxProducerCosts = value;
				NotifyOfPropertyChange("MaxProducerCosts");
			}
		}

		public Price Price
		{
			get
			{
				if (CurrentOffer == null)
					return null;
				return Session.Load<Price>(CurrentOffer.Price.Id);
			}
		}

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				NotifyOfPropertyChange("CurrentFilter");
			}
		}

		public List<string> Regions
		{
			get { return regions; }
			set
			{
				regions = value;
				NotifyOfPropertyChange("Regions");
			}
		}

		public string CurrentRegion
		{
			get { return currentRegion; }
			set
			{
				currentRegion = value;
				NotifyOfPropertyChange("CurrentRegion");
			}
		}

		public bool GroupByProduct
		{
			get { return groupByProduct; }
			set
			{
				groupByProduct = value;
				Offers = Sort(Offers);
				NotifyOfPropertyChange("GroupByProduct");
			}
		}

		public decimal RetailCost
		{
			get
			{
				if (CurrentOffer == null)
					return 0;
				return Math.Round(CurrentOffer.Cost * (1 + RetailMarkup / 100), 2);
			}
		}

		public decimal RetailMarkup
		{
			get
			{
				return retailMarkup == 0 ? MarkupConfig.Calculate(markups, CurrentOffer) : retailMarkup;
			}
			set
			{
				retailMarkup = value;
				NotifyOfPropertyChange("RetailMarkup");
			}
		}

		public List<SentOrderLine> HistoryOrders
		{
			get { return historyOrders; }
			set
			{
				historyOrders = value;
				NotifyOfPropertyChange("HistoryOrders");
			}
		}

		private List<Offer> Sort(List<Offer> offers)
		{
			if (offers == null)
				return null;

			if (GroupByProduct) {
				return SortByMinCostInGroup(offers, o => o.ProductId);
			}
			else {
				return SortByMinCostInGroup(offers, o => o.CatalogId);
			}
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public void Print()
		{
			var rows = offers.Select((o, i) => {
				return new object[] {
					i,
					o.ProductSynonym,
					o.ProducerSynonym,
					o.Cost,
					o.OrderCount,
					o.OrderSum
				};
			});

			var doc = BuildDocument(rows);
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

			var outputXps = "output.xps";
			using (var stream = File.Create(outputXps)) {
				var factory = new XpsSerializerFactory();
				var writer = factory.CreateSerializerWriter(stream);
				writer.Write(paginator);
			}
			Process.Start(outputXps);

			//var server = new LocalPrintServer();
			//var queue = server.DefaultPrintQueue;
			//var writer = PrintQueue.CreateXpsDocumentWriter(queue);
			//
			//writer.Write(paginator);
		}

		private static FlowDocument BuildDocument(IEnumerable<object[]> rows)
		{
			var doc = new FlowDocument();
			var table = new Table();
			table.CellSpacing = 0;
			table.Columns.Add(new TableColumn());
			table.Columns.Add(new TableColumn());
			table.Columns.Add(new TableColumn());
			table.Columns.Add(new TableColumn());
			table.Columns.Add(new TableColumn());
			table.Columns.Add(new TableColumn());
			var tableRowGroup = new TableRowGroup();
			table.RowGroups.Add(tableRowGroup);

			foreach (var data in rows) {
				var row = new TableRow();
				tableRowGroup.Rows.Add(row);

				for (var i = 0; i < data.Length; i++) {
					var cell = new TableCell(new Paragraph(new Run(data[i].ToString())));
					cell.BorderBrush = Brushes.Black;
					cell.BorderThickness = new Thickness(1);
					row.Cells.Add(cell);
				}
			}
			doc.Blocks.Add(table);
			return doc;
		}

		public void ShowCatalog()
		{
			if (CurrentOffer == null)
				return;

			var price = CurrentOffer.Price;
			var catalogViewModel = new PriceViewModel {
				CurrentPrice = price
			};
			var offerViewModel = new PriceOfferViewModel(price, catalogViewModel.ShowLeaders);
			offerViewModel.CurrentOffer = offerViewModel.Offers.FirstOrDefault(o => o.Id == CurrentOffer.Id);

			Shell.NavigateAndReset(catalogViewModel, offerViewModel);
		}

		public bool CanExport
		{
			get { return true; }
		}

		public IResult Export()
		{
			var view = (UserControl) GetView();
			var grid = (DataGrid)view.DeepChildren().OfType<Controls.DataGrid>().First(g => g.Name == "Offers");
			var columns = grid.Columns;
			var filename = Path.ChangeExtension(Path.GetRandomFileName(), "xls");
			using(var file = File.OpenWrite(filename)) {
				var book = new HSSFWorkbook();
				var sheet = book.CreateSheet("Экспорт");
				var rowIndex = 0;
				var row = sheet.CreateRow(rowIndex++);
				for(var i = 0; i < columns.Count; i++) {
					row.CreateCell(i).SetCellValue(columns[i].Header.ToString());
				}
				foreach (var offer in Offers) {
					row = sheet.CreateRow(rowIndex++);
					for(var i = 0; i < columns.Count; i++) {
						row.CreateCell(i).SetCellValue(GetValue(columns[i], offer));
					}
				}
				book.Write(file);
			}

			return new OpenFileResult(filename);
		}

		private string GetValue(DataGridColumn column, object offer)
		{
			var path = ((Binding)((DataGridTextColumn)column).Binding).Path.Path;
			var parts = path.Split('.');

			var value = offer;
			foreach (var part in parts) {
				if (value == null)
					return "";
				var type = value.GetType();
				var property = type.GetProperty(part);
				if (property == null)
					return "";
				value = property.GetValue(value, null);
			}
			if (value == null)
				return "";
			return value.ToString();
		}
	}

	public class OpenFileResult : IResult
	{
		public string Filename;

		public OpenFileResult(string filename)
		{
			Filename = filename;
		}

		//TODO: Обработка ошибок?
		public void Execute(ActionExecutionContext context)
		{
			Process.Start("excel", Filename);
			Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}