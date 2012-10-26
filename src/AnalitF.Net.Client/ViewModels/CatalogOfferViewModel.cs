﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
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
				.Subscribe(_ => RaisePropertyChangedEventImmediately("RetailMarkup"));

			this.ObservableForProperty(m => m.RetailMarkup)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("RetailCost"));

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("Price"));

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
			var queryable = Session.Query<Offer>().Where(o => o.CatalogId == CurrentCatalog.Id);
			if (CurrentRegion != allRegionLabel) {
				queryable = queryable.Where(o => o.RegionName == CurrentRegion);
			}
			if (CurrentProducer != AllProducerLabel) {
				queryable = queryable.Where(o => o.ProducerSynonym == CurrentProducer);
			}
			if (CurrentFilter == Filters[1]) {
				queryable = queryable.Where(o => o.Price.BasePrice);
			}
			if (CurrentFilter == Filters[2]) {
				queryable = queryable.Where(o => !o.Price.BasePrice);
			}

			var offers = queryable.ToList();
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
				RaisePropertyChangedEventImmediately("MaxProducerCosts");
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
				RaisePropertyChangedEventImmediately("CurrentFilter");
			}
		}

		public List<string> Regions
		{
			get { return regions; }
			set
			{
				regions = value;
				RaisePropertyChangedEventImmediately("Regions");
			}
		}

		public string CurrentRegion
		{
			get { return currentRegion; }
			set
			{
				currentRegion = value;
				RaisePropertyChangedEventImmediately("CurrentRegion");
			}
		}

		public bool GroupByProduct
		{
			get { return groupByProduct; }
			set
			{
				groupByProduct = value;
				Offers = Sort(Offers);
				RaisePropertyChangedEventImmediately("GroupByProduct");
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
				RaisePropertyChangedEventImmediately("RetailMarkup");
			}
		}

		public List<SentOrderLine> HistoryOrders
		{
			get { return historyOrders; }
			set
			{
				historyOrders = value;
				RaisePropertyChangedEventImmediately("HistoryOrders");
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
			throw new NotImplementedException();
		}

		public bool CanExport
		{
			get { return true; }
		}

		public void Export()
		{
			var view = (UserControl) GetView();
			var grid = (DataGrid)view.DeepChildren().OfType<Controls.DataGrid>().First(g => g.Name == "Offers");
			var columns = grid.Columns;
			var filename = Path.GetTempFileName();
			Console.WriteLine(filename);
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
}