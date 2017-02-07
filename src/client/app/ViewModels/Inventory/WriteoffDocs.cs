using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Views.Inventory;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class WriteoffDocs : BaseScreen2
	{
		public WriteoffDocs()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanOpen.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.NotPosted;
			});
			DisplayName = "Списание";
			TrackDb(typeof(WriteoffDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<WriteoffDoc>> Items { get; set; }
		public NotifyValue<WriteoffDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanOpen { get; set; }

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

		public IEnumerable<IResult> Create()
		{
			if (Address == null)
				yield break;
			var doc = new WriteoffDoc(Address);
			yield return new DialogResult(new CreateWriteoffDoc(doc));
			Session.Save(doc);
			Update();
			Shell.Navigate(new EditWriteoffDoc(doc.Id));
		}

		public void Open()
		{
			if (!CanOpen)
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
			Open();
		}
	}
}
