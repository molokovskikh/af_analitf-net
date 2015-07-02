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
		}

		public NotifyValue<List<JournalRecord>> Items { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Items = Bus.Listen<JournalRecord>()
				.SubscribeOn(UiScheduler)
				.Merge(Observable.Return<JournalRecord>(null))
				.Select(r => Session.Query<JournalRecord>().OrderByDescending(o => o.CreateAt).ToList())
				.ToValue();
		}

		public IResult Open(JournalRecord record)
		{
			return new OpenResult(record.Filename);
		}

		public IResult Show(JournalRecord record)
		{
			return new SelectResult(record.Filename);
		}
	}
}