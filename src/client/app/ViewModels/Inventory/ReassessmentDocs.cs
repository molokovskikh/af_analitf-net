using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReassessmentDocs : BaseScreen2
	{
		public ReassessmentDocs()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanEdit.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.NotPosted;
			});
			DisplayName = "����������";
			TrackDb(typeof(ReassessmentDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<List<ReassessmentDoc>> Items { get; set; }
		public NotifyValue<ReassessmentDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanEdit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.SelectMany(_ => RxQuery(s => s.Query<ReassessmentDoc>()
					.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1))
					.Fetch(x => x.Address)
					.OrderByDescending(x => x.Date).ToList()))
				.Subscribe(Items);
		}

		public void Create()
		{
			Shell.Navigate(new EditReassessmentDoc(new ReassessmentDoc(Address)));
		}

		public void Edit()
		{
			if (!CanEdit)
				return;
			Shell.Navigate(new EditReassessmentDoc(CurrentItem.Value.Id));
		}

		public async Task Delete()
		{
			if (!CanDelete)
				return;
			if (!Confirm("������� ��������� ��������?"))
				return;
			await Env.Query(s => s.Delete(CurrentItem.Value));
			Update();
		}

		public void EnterItem()
		{
			Edit();
		}
	}
}