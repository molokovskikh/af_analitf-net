using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class Journal : BaseScreen
	{
		public Journal()
		{
			DisplayName = "Журнал загрузок";
			Items = new NotifyValue<List<JournalRecord>>();
		}

		public NotifyValue<List<JournalRecord>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Bus.Listen<JournalRecord>()
				.Merge(Observable.Return<JournalRecord>(null))
				.Select(_ => RxQuery(x => x.Query<JournalRecord>().OrderByDescending(o => o.CreateAt).ToList()))
				.ObserveOn(UiScheduler)
				.Switch()
				.Subscribe(Items, CloseCancellation.Token);
		}

		public IResult Open(JournalRecord record)
		{
			return new OpenResult(record.Filename);
		}
	}
}