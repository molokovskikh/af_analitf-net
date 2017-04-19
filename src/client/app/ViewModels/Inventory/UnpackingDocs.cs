using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class UnpackingDocs : BaseScreen2
	{
		private ReactiveCollection<UnpackingDoc> items;

		public UnpackingDocs()
		{
			Items = new ReactiveCollection<UnpackingDoc>();
			IsAll = new NotifyValue<bool>(true);
			Begin.Value = DateTime.Today.AddDays(-30);
			End.Value = DateTime.Today;
			CurrentItem.Subscribe(x => {
				CanOpen.Value = x != null;
				CanDelete.Value = x?.Status == DocStatus.NotPosted;
				CanPost.Value = x?.Status == DocStatus.NotPosted;
				CanUnPost.Value = x?.Status == DocStatus.Posted && !x.Lines.Any(y => y.Moved);
			});
			DisplayName = "Распаковка";
			TrackDb(typeof(UnpackingDoc));
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		[Export]
		public ReactiveCollection<UnpackingDoc> Items
		{
			get { return items; }
			set
			{
				items = value;
				NotifyOfPropertyChange(nameof(Items));
			}
		}
		public NotifyValue<UnpackingDoc> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanOpen { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }
		public NotifyValue<bool> IsAll { get; set; }
		public NotifyValue<bool> IsNotPosted { get; set; }
		public NotifyValue<bool> IsPosted { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			DbReloadToken
				.Merge(Begin.Select(x => (object)x))
				.Merge(End.Select(x => (object)x))
				.Merge(IsNotPosted.Changed())
				.Merge(IsPosted.Changed())
				.Subscribe(_ => Update(), CloseCancellation.Token);
		}

		public override void Update()
		{
			Session.Clear();
			var query = Session.Query<UnpackingDoc>()
				.Where(x => x.Date > Begin.Value && x.Date < End.Value.AddDays(1));

			if (IsNotPosted)
				query = query.Where(x => x.Status == DocStatus.NotPosted);
			else if (IsPosted)
				query = query.Where(x => x.Status == DocStatus.Posted);

			var items = query.Fetch(x => x.Address)
				.OrderByDescending(x => x.Date)
				.ToList();

			Items = new ReactiveCollection<UnpackingDoc>(items)
			{
				ChangeTrackingEnabled = true
			};
		}

		public void Open()
		{
			if (!CanOpen)
				return;
			Shell.Navigate(new EditUnpackingDoc(CurrentItem.Value.Id));
		}

		public async Task Delete()
		{
			if (!CanDelete)
				return;
			if (!Confirm("Удалить выбранный документ?"))
				return;
			CurrentItem.Value.BeforeDelete();
			await Env.Query(s => s.Delete(CurrentItem.Value));
			Update();
			CurrentItem.Refresh();
		}

		public void Post()
		{
			if (!Confirm("Провести выбранный документ?"))
				return;
			CurrentItem.Value.Post();
			Session.Flush();
			Update();
			CurrentItem.Refresh();
		}

		public void UnPost()
		{
			if (!Confirm("Распровести выбранный документ?"))
				return;
			CurrentItem.Value.UnPost();
			Session.Flush();
			Update();
			CurrentItem.Refresh();
		}

		public void EnterItem()
		{
			Open();
		}
	}
}
