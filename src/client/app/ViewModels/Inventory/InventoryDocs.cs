using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class InventoryDocs : BaseScreen2
	{
		public InventoryDocs()
		{
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x != null;
			});
			DisplayName = "Инвентаризация";
			Bus.Listen<string>("db").Where(x => x == nameof(InventoryDoc))
				.Subscribe(_ => UpdateOnActivate = true, CloseCancellation.Token);
		}

		public NotifyValue<List<InventoryDoc>> Items { get; set; }
		public NotifyValue<InventoryDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.SelectMany(_ => RxQuery(s => s.Query<InventoryDoc>()
					.Fetch(x => x.Address)
					.OrderByDescending(x => x.Date).ToList()))
				.Subscribe(Items);
		}

		public void Create()
		{
			Shell.Navigate(new EditInventoryDoc(new InventoryDoc(Address)));
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new EditInventoryDoc(CurrentItem.Value.Id));
		}

		public void Delete()
		{
			StatelessSession.Delete(CurrentItem.Value);
			DbReloadToken.Value = new object();
		}

		public void EnterItem()
		{
			Edit();
		}
	}
}