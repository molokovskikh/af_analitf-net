using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using Ionic.Zip;
using Microsoft.Runtime.CompilerServices;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class Mails : BaseScreen
	{
		private Dictionary<string, Func<Mail, object>> sortmap
			= new Dictionary<string, Func<Mail, object>> {
				{ "Сортировка: Дата", m => m.SentAt },
				{ "Сортировка: Тема", m => m.Subject },
				{ "Сортировка: Отправитель", m => m.Sender },
				{ "Сортировка: Важность", m => m.IsImportant }
			};
		private List<Mail> mails = new List<Mail>();

		public Mails()
		{
			DisplayName = "Минипочта";
			Sort = new[] {
				"Сортировка: Дата",
				"Сортировка: Тема",
				"Сортировка: Отправитель",
				"Сортировка: Важность",
			};

			Term = new NotifyValue<string>();
			CurrentItem = new NotifyValue<Mail>();
			SelectedItems = new ObservableCollection<Mail>();
			CurrentSort = new NotifyValue<string>("Сортировка: Дата");
			IsAsc = new NotifyValue<bool>(false);

			Items = Term.Changed()
				.Throttle(TimeSpan.FromMilliseconds(100), UiScheduler)
				.Merge(CurrentSort.Changed())
				.Merge(IsAsc.Changed())
				.ToValue(_ => Apply());
			CanDelete = SelectedItems.Changed().ToValue(_ => SelectedItems.Count > 0);
			var updateStat = Items.ObservableForProperty(i => i.Value)
				.Select(l => l.Value.Changed())
				.Switch();
			OnCloseDisposable.Add(updateStat.Subscribe(_ => Shell.NewMailsCount.Value = mails.Count(m => m.IsNew)));
		}

		public NotifyValue<string> CurrentSort { get; set; }

		public string[] Sort { get; set; }

		public NotifyValue<bool> IsAsc { get; set; }

		public NotifyValue<string> Term { get; set; }

		public NotifyValue<BindingList<Mail>> Items { get; set; }

		public NotifyValue<Mail> CurrentItem { get; set; }

		public ObservableCollection<Mail> SelectedItems { get; set; }

		public NotifyValue<bool> CanDelete { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			var pendingDownloads = Shell.PendingDownloads;
			foreach (var attachment in pendingDownloads.OfType<Attachment>().ToArray()) {
				attachment.Session = Session;
				attachment.Entry = NHHelper.Reassociate(Session, attachment, attachment.Entry);
			}

			Mail.TrackIsNew(UiScheduler, CurrentItem);
			mails = Session.Query<Mail>().ToList();
			Items.Recalculate();
		}

		private BindingList<Mail> Apply()
		{
			var sort = sortmap.GetValueOrDefault(CurrentSort.Value, sortmap.Values.First());
			IEnumerable<Mail> items = mails;
			if (!String.IsNullOrEmpty(Term))
				items = mails.Where(m => (m.Body ?? "").IndexOf(Term.Value, StringComparison.CurrentCultureIgnoreCase) >= 0
					|| (m.Subject ?? "").IndexOf(Term.Value, StringComparison.CurrentCultureIgnoreCase) >= 0
					|| (m.Sender ?? "").IndexOf(Term.Value, StringComparison.CurrentCultureIgnoreCase) >= 0);
			if (IsAsc)
				return new BindingList<Mail>(items.OrderBy(sort).ToList());
			else
				return new BindingList<Mail>(items.OrderByDescending(sort).ToList());
		}

		public void Delete()
		{
			using(var cleaner = new FileCleaner()) {
				foreach (var mail in SelectedItems.ToArray()) {
					Session.Delete(mail);
					mails.Remove(mail);
					Items.Value.Remove(mail);
					cleaner.Watch(mail.Attachments.Select(a => a.LocalFilename).Where(f => f != null));
				}
			}
			Items.Refresh();
		}
	}
}