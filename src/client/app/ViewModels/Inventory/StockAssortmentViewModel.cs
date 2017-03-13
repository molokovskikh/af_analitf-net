using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate;
using System.Reactive.Linq;
using ReactiveUI;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Util;
using Common.Tools.Calendar;
using AnalitF.Net.Client.ViewModels.Parts;

namespace AnalitF.Net.Client.ViewModels.Inventory
{

	public class AddressStock
	{
		public Address Address { get; set; }
		public decimal Quantity { get; set; }
		public decimal ReservedQuantity { get; set; }
	}

	public class StockEx
	{
		public Stock Stock { get; set; }
		public WaybillLine WaybillLine { get; set; }
	}

	public class StockAssortmentViewModel : BaseScreen
	{
		public NotifyValue<List<Catalog>> Catalogs { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }
		public NotifyValue<List<AddressStock>> AddressStock { get; set; }
		public NotifyValue<AddressStock> CurrentAddressStock { get; set; }
		public NotifyValue<List<StockEx>> Stocks { get; set; }
		public NotifyValue<StockEx> CurrentStock { get; set; }
		public NotifyValue<List<StockAction>> StockActions { get; set; }
		public NotifyValue<StockAction> CurrentStockAction { get; set; }

		public QuickSearch<Catalog> CatalogsSearch { get; }

		public StockAssortmentViewModel()
		{
			DisplayName = "Ассортимент товаров";
			InitFields();

			CatalogsSearch = new QuickSearch<Catalog>(UiScheduler,
				v => Catalogs.Value.FirstOrDefault(n => n.FullName.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				c => CurrentCatalog.Value = c);
		}

		public override void PostActivated()
		{
			base.PostActivated();
			int a = 0;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(s => {
					return s.Query<Catalog>()
							.Join(s.Query<Stock>(),
								catalog => catalog.Id,
								stock => stock.ProductId,
								(catalog, stock) => new { catalog, stock })
							.Where(p => p.stock.Status == StockStatus.Available)
							.Select(c => c.catalog)
							.OrderBy(c => c.Name)
							.Distinct()
							.ToArray()
							.ToList();
				}).Subscribe(Catalogs);

			Catalogs
				.Changed()
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Subscribe(_ =>
				{
					CurrentCatalog.Value = (Catalogs.Value ?? Enumerable.Empty<Catalog>()).FirstOrDefault();
				});

			CurrentCatalog
				.Changed()
				.Select(_=>RxQuery(LoadAddressStock))
				.Switch()
				.Subscribe(AddressStock, CloseCancellation.Token);

			AddressStock
				.Changed()
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Subscribe(_ =>
				{
					CurrentAddressStock.Value = (AddressStock.Value ?? Enumerable.Empty<AddressStock>()).FirstOrDefault();
				});

			CurrentAddressStock
				.Changed()
				.Select(_ => RxQuery(LoadStoks))
				.Switch()
				.Subscribe(Stocks, CloseCancellation.Token);

			Stocks
				.Changed()
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Subscribe(_ =>
				{
					CurrentStock.Value = (Stocks.Value ?? Enumerable.Empty<StockEx>()).FirstOrDefault();
				});

			CurrentStock
				.Changed()
				.Select(_ => RxQuery(LoadStockActions))
				.Switch()
				.Subscribe(StockActions, CloseCancellation.Token);

			StockActions
				.Changed()
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Subscribe(_ =>
				{
					CurrentStockAction.Value = (StockActions.Value ?? Enumerable.Empty<StockAction>()).FirstOrDefault();
				});
		}

		public List<AddressStock> LoadAddressStock(IStatelessSession session)
		{
			return LoadAddressStock(session, Cache, Settings.Value, CurrentCatalog.Value);
		}

		public static List<AddressStock> LoadAddressStock(IStatelessSession session, SimpleMRUCache cache,
			Settings settings, Catalog Catalog)
		{
			if (Catalog == null )
				return new List<AddressStock>();
			var query = session.Query<Stock>()
				.Where(p => p.Status == StockStatus.Available)
				.Where(p => p.ProductId == Catalog.Id);
			return query.Fetch(y => y.Address).OrderBy(y => y.Product)
				.GroupBy(l => l.Address)
				.Select(cl => new AddressStock
				{
					Address = cl.First().Address,
					Quantity = cl.Sum(c => c.Quantity),
					ReservedQuantity = cl.Sum(c => c.ReservedQuantity)
				}).ToList();
		}

		public List<StockEx> LoadStoks(IStatelessSession session)
		{
			return LoadStoks(session, Cache, Settings.Value, CurrentAddressStock.Value, CurrentCatalog.Value);
		}

		public static List<StockEx> LoadStoks(IStatelessSession session, SimpleMRUCache cache,
			Settings settings, AddressStock AddressStock, Catalog Catalog)
		{
			if (AddressStock == null)
				return new List<StockEx>();
			var query = session.Query<Stock>()
				.Fetch(x => x.Address)
				//.Fetch(x => x.WaybillLine)
				.Where(x => x.Status == StockStatus.Available)
				.Where(x => x.ProductId == Catalog.Id)
				.Join(session.Query<WaybillLine>(),
							stock => stock.WaybillLineId,
							waybillLine => waybillLine.Id,
							(stock, waybillLine) => new StockEx { Stock = stock, WaybillLine = waybillLine });
			return query
			.ToList();

		}

		public List<StockAction> LoadStockActions(IStatelessSession session)
		{
			return LoadStockActions(session, Cache, Settings.Value, CurrentStock.Value);
		}

		public static List<StockAction> LoadStockActions(IStatelessSession session, SimpleMRUCache cache,
			Settings settings, StockEx Stock)
		{
			if (Stock == null)
				return new List<StockAction>();
			var query = session.Query<StockAction>()
				.Where(x => x.ClientStockId == Stock.Stock.Id);
			return query
			.ToList();

		}
	}
}
