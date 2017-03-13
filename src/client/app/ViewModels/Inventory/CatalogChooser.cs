using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Dapper;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CatalogChooser : Screen, ICancelable
	{
		public CatalogChooser(string term, Address address)
		{
			BaseScreen.InitFields(this);
			DisplayName = "Выберете товар";
			WasCancelled = true;
			var env = Config.Env.Current;
			SearchBehavior = new SearchBehavior(env);
			SearchBehavior.ActiveSearchTerm.Value = term;

			SearchBehavior.ActiveSearchTerm
				.Do(_ => IsLoading.Value = true)
				.Select(_ => env.RxQuery(s => {
					var sql = @"
select c.Id as CatalogId, cn.Name, c.Form, c.HaveOffers, c.VitallyImportant
from Catalogs c
	join CatalogNames cn on cn.Id = c.NameId
		join ( select CatalogId from Stocks where AddressId = @addressId and Quantity > 0 group by CatalogId ) s on s.CatalogId = c.Id
where cn.Name like @term or c.Form like @term
order by cn.Name, c.Form";
					return s.Connection.Query<CatalogDisplayItem>(sql, new {
						term = "%" + SearchBehavior.ActiveSearchTerm.Value + "%",
						addressId = address.Id
					}).ToList();
				}))
				.Switch()
				.Do(_ => IsLoading.Value = false)
				.Subscribe(Items);

			Items.Subscribe(_ => {
				CurrentItem.Value = (Items.Value ?? Enumerable.Empty<CatalogDisplayItem>()).FirstOrDefault();
			});
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }
		public NotifyValue<List<CatalogDisplayItem>> Items { get; set;}
		public NotifyValue<CatalogDisplayItem> CurrentItem { get; set; }

		public bool WasCancelled { get; set; }

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}