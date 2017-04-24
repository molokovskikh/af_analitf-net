using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate;
using ReactiveUI;
using NHibernate.Linq;
using System.Linq;
using System.Collections.Generic;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditUnpackingDoc : BaseScreen2
	{

		private uint id;

		private EditUnpackingDoc()
		{
			Lines = new NotifyValue<IList<UnpackingLine>>();
			Session.FlushMode = FlushMode.Never;
		}

		public EditUnpackingDoc(uint id)
			: this()
		{
			DisplayName = "Детализация распаковки";
			InitDoc(Session.Load<UnpackingDoc>(id));
			//Lines.AddRange(Doc.Lines);
			this.id = id;
		}

		public UnpackingDoc Doc { get; set; }
		public NotifyValue<IList<UnpackingLine>> Lines { get; set; }
		public NotifyValue<UnpackingLine> CurrentLine { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Doc.Id == 0)
				Doc.Address = Address;
			RxQuery(x => {
				return x.Query<UnpackingLine>()
					.Fetch(y => y.DstStock)
					.Where(y => y.UnpackingDocId == id)
					.ToList()
					.ToObservableCollection();
			})
				.Subscribe(Lines);
		}

		protected override void OnDeactivate(bool close)
		{
			Save();
			base.OnDeactivate(close);
		}

		public override void Update()
		{
			Session.Refresh(Doc);
		}

		private void InitDoc(UnpackingDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			docStatus.Select(x => x.Value == DocStatus.NotPosted).Subscribe(CanPost);
			docStatus.Select(x => x.Value == DocStatus.Posted).Subscribe(CanUnPost);
		}

		private void Save()
		{
			Doc.UpdateStat();
			if (Doc.Id == 0)
				Session.Save(Doc);
			else
				Session.Update(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(UnpackingDoc), "db");
		}
	}
}