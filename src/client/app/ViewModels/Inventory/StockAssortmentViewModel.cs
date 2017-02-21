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

namespace AnalitF.Net.Client.ViewModels.Inventory
{

	public class AddressStock
	{
		public Address Address { get; set; }
		public decimal Quantity { get; set; }
		public decimal ReservedQuantity { get; set; }
	}
	public class StockAssortmentViewModel : BaseScreen
	{
		public NotifyValue<List<Catalog>> Catalogs { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }
		public NotifyValue<List<AddressStock>> AddressStock { get; set; }
		public NotifyValue<AddressStock> CurrentAddressStock { get; set; }

		private uint Id;

		public StockAssortmentViewModel()
		{
			DisplayName = "111";
			InitFields();
		}


		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(s =>
			{
				return s.Query<Catalog>()
					.Join(s.Query<Stock>(),
						catalog => catalog.Id,
						stock => stock.ProductId,
						(catalog, stock) => new { catalog, stock })
					.Where(p => p.stock.Status == StockStatus.Available)
					.Select(c => c.catalog)
					//.Distinct()
					.OrderBy(c => c.Name)
					.ToList();
			}).Subscribe(Catalogs);

			CurrentCatalog
				.Select(_ => RxQuery(LoadAddressStock))
				.Switch()
				.Subscribe(AddressStock, CloseCancellation.Token);

		}

		protected override void OnActivate()
		{
			base.OnActivate();
		}

		public List<AddressStock> LoadAddressStock(IStatelessSession session)
		{
			var list = LoadAddressStock(session, Cache, Settings.Value, CurrentCatalog.Value);
			CurrentAddressStock.Value = list.FirstOrDefault();
			return list;
		}

		public static List<AddressStock> LoadAddressStock(IStatelessSession session, SimpleMRUCache cache,
			Settings settings, Catalog Catalog)
		{
			if (Catalog == null )
				return new List<AddressStock>();
			var query = session.Query<Stock>()
				//.Where(y => y.Quantity != 0 || y.ReservedQuantity != 0)
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
	}
}
