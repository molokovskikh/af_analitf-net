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
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x != null;
			});
			DisplayName = "Излишки";
			TrackDb(typeof(InventoryDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<InventoryDoc>> Items { get; set; }
		public NotifyValue<InventoryDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.SelectMany(_ => RxQuery(s => s.Query<InventoryDoc>()
					.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1))
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
			Update();
		}

		public void EnterItem()
		{
			Edit();
		}
	}
}