using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class WriteoffDocs : BaseScreen2
	{
		public WriteoffDocs()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.Opened;
			});
			DisplayName = "Списание";
			TrackDb(typeof(WriteoffDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<WriteoffDoc>> Items { get; set; }
		public NotifyValue<WriteoffDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.SelectMany(_ => RxQuery(s => s.Query<WriteoffDoc>()
					.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1))
					.Fetch(x => x.Address)
					.Fetch(x => x.Reason)
					.OrderByDescending(x => x.Date).ToList()))
				.Subscribe(Items);
		}

		public void Create()
		{
			Shell.Navigate(new EditWriteoffDoc(new WriteoffDoc(Address)));
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new EditWriteoffDoc(CurrentItem.Value.Id));
		}

		public void Delete()
		{
			Env.Query(s => s.Delete(CurrentItem.Value)).LogResult();
			Update();
		}

		public void EnterItem()
		{
			Edit();
		}
	}
}
