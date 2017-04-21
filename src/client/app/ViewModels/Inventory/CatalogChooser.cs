using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Dapper;
using NHibernate.Linq;
using AnalitF.Net.Client.ViewModels.Dialogs;
using NPOI.SS.Formula.Functions;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CatalogChooser : BaseScreen, ICancelable
	{
		public class CatalogLine : CatalogDisplayItem
		{
			public decimal? MinCost
			{
				get
				{
					if (MinRetailCost == null)
						return MinEquivalentCost;
					if (MinEquivalentCost == null)
						return MinRetailCost;
					return Math.Min(MinRetailCost.Value, MinEquivalentCost.Value);
				}
			}

			public decimal? MinRetailCost { get; set; }
			public decimal? MinEquivalentCost { get; set; }
		}

		public CatalogChooser(string term, Address address)
		{
			BaseScreen.InitFields(this);
			DisplayName = "Выберите товар";
			WasCancelled = true;
			var env = Config.Env.Current;
			SearchBehavior = new SearchBehavior(env);
			SearchBehavior.ActiveSearchTerm.Value = term;

			SearchBehavior.ActiveSearchTerm
				.Do(_ => IsLoading.Value = true)
				.Select(_ => env.RxQuery(s => {
					var sql = @"
drop temporary table if exists StockCatalogs;
create temporary table StockCatalogs(
	CatalogId int unsigned,
	MinRetailCost decimal(12, 2),
	primary key(CatalogId)
);
insert into StockCatalogs
select CatalogId, min(RetailCost) as MinRetailCost
from Stocks
where AddressId = @addressId and Quantity > 0
	and Status = @stockStatus
	and RetailCost > 0
group by CatalogId;

drop temporary table if exists GroupByType;
create temporary table GroupByType (
	MnnId int unsigned,
	Type int unsigned,
	MinRetailCost decimal(12, 2),
	primary key(MnnId, Type)
)
select cn.MnnId, c.Type, min(MinRetailCost) as MinRetailCost
from StockCatalogs s
	join Catalogs c on c.Id = s.CatalogId
		join CatalogNames cn on cn.Id = c.NameId
where cn.MnnId is not null
	and c.Type is not null
group by cn.MnnId, c.Type;

select c.Id as CatalogId, cn.Name, c.Form, c.HaveOffers, c.VitallyImportant,
	s.MinRetailCost as MinRetailCost,
	t.MinRetailCost as MinEquivalentCost
from Catalogs c
	join CatalogNames cn on cn.Id = c.NameId
		left join StockCatalogs s on s.CatalogId = c.Id
		left join GroupByType t on t.MnnId = cn.MnnId and t.Type = c.TYpe
where (cn.Name like @term or c.Form like @term) and (s.CatalogId is not null or t.MnnId is not null)
order by cn.Name, c.Form;

drop temporary table GroupByType;
drop temporary table StockCatalogs;
";
					return s.Connection.Query<CatalogLine>(sql, new {
						term = "%" + SearchBehavior.ActiveSearchTerm.Value + "%",
						addressId = address.Id,
						stockStatus = StockStatus.Available
					}).ToList();
				}))
				.Switch()
				.Do(_ => IsLoading.Value = false)
				.Subscribe(Items);

			Items.Subscribe(_ => {
				CurrentItem.Value = (Items.Value ?? Enumerable.Empty<CatalogDisplayItem>()).FirstOrDefault();
			});

			CurrentItem
				.SelectMany(x => Env.RxQuery(s => {
					if (x == null)
						return null;
					var catalogId = x.CatalogId;
					return s.Query<Catalog>()
						.Fetch(c => c.Name)
						.ThenFetch(n => n.Mnn)
						.FirstOrDefault(c => c.Id == catalogId);
				}))
				.Subscribe(CurrentCatalog, CloseCancellation.Token);
			CurrentCatalog.Select(x => x?.Name?.Description != null).Subscribe(CanShowDescription);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }
		public NotifyValue<List<CatalogLine>> Items { get; set;}
		public NotifyValue<CatalogDisplayItem> CurrentItem { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }
		public NotifyValue<bool> CanShowDescription { get; set; }

		public bool WasCancelled { get; set; }

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			WasCancelled = false;
			TryClose();
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;
			Manager.ShowDialog(new DocModel<ProductDescription>(CurrentCatalog.Value.Name.Description.Id));
		}
	}
}