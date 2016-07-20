using System;
using System.Collections.ObjectModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class CheckDetails : BaseScreen2
	{
		private uint id;

		public CheckDetails()
		{
			DisplayName = "Чек";
		}

		public CheckDetails(uint id)
			: this()
		{
			this.id = id;
		}

		public NotifyValue<Check> Header { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public NotifyValue<ObservableCollection<CheckLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Header.Value == null)
			{
				RxQuery(x => x.Query<Check>()
					.FirstOrDefault(y => y.Id == id))
					.Subscribe(Header);
				RxQuery(x =>
				{
					return x.Query<CheckLine>().Where(y => y.CheckId == id).OrderBy(y => y.ProductId)
						.ToList()
						.ToObservableCollection();
				})
					.Subscribe(Lines);
			}
		}
	}
}
