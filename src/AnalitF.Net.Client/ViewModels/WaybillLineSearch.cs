using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaybillLineSearch : BaseScreen
	{
		private DateTime begin;
		private DateTime end;

		public WaybillLineSearch(DateTime begin, DateTime end)
		{
			DisplayName = "Поиск товара в накладных";
			this.begin = begin;
			this.end = end;

			Lines = new NotifyValue<List<WaybillLine>>();
			SearchBehavior = new SearchBehavior(OnCloseDisposable, UiScheduler, Scheduler, Update);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<List<WaybillLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Update();
		}

		public void Update()
		{
			var query = StatelessSession.Query<WaybillLine>()
				.Where(l => l.Waybill.WriteTime >= begin && l.Waybill.WriteTime < end);

			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term))
				query = query.Where(m => m.SerialNumber.Contains(term));

			Lines.Value = query
				.OrderBy(l => l.Product)
				.Fetch(l => l.Waybill)
				.ThenFetch(w => w.Supplier)
				.ToList();
		}

		public IResult ClearSearch()
		{
			return SearchBehavior.ClearSearch();
		}

		public IResult Search()
		{
			return SearchBehavior.Search();
		}
	}
}