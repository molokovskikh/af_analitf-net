using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReceivingOrders : BaseScreen2
	{
		public ReceivingOrders()
		{
			DisplayName = "Приход от поставщика";
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
		}

		public NotifyValue<List<ReceivingOrder>> Items { get; set; }
		public NotifyValue<ReceivingOrder> CurrentItem { get; set; }
		public NotifyValue<IList<Selectable<Supplier>>> Suppliers { get; set; }
		public NotifyValue<IList<Selectable<Address>>> AddressesFilter { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<Supplier>().OrderBy(y => y.Name).ToArray().Select(y => new Selectable<Supplier>(y)).ToList())
				.Subscribe(Suppliers);
			RxQuery(x => x.Query<Address>().OrderBy(y => y.Name).ToArray().Select(y => new Selectable<Address>(y)).ToList())
				.Subscribe(AddressesFilter);

			AddressesFilter.FilterChanged()
				.Merge(Suppliers.FilterChanged())
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.SelectMany(_ => RxQuery(s => {
					var query = s.Query<ReceivingOrder>().Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1));
					if (Suppliers.IsFiltred()) {
						var values = Suppliers.GetValues();
						query = query.Where(x => values.Contains(x.Supplier));
					}
					if (AddressesFilter.IsFiltred()) {
						var values = AddressesFilter.GetValues();
						query = query.Where(x => values.Contains(x.Address));
					}
					return query
						.Fetch(y => y.Supplier)
						.Fetch(y => y.Address)
						.OrderByDescending(y => y.Date)
						.ToList();
				}))
				.Subscribe(Items);
		}

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			Shell.Navigate(new ReceivingDetails(CurrentItem.Value.Id));
		}
	}
}