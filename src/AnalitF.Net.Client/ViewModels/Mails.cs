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
			Sort = new [] {
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

			var update = Term.Changed()
				.Throttle(TimeSpan.FromMilliseconds(100), UiScheduler)
				.Merge(CurrentSort.Changed())
				.Merge(IsAsc.Changed());
			Items = new NotifyValue<BindingList<Mail>>(Apply, update);
			var countChanged = SelectedItems.Changed();
			CanDelete = new NotifyValue<bool>(() => SelectedItems.Count > 0, countChanged);
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

		//todo - если соединение быстрое то загрузка происходит так быстро что элементы управления мигают
		//черная магия будь бдителен все обработчики живут дольше чем форма и могут быть вызваны после того как форма была закрыта
		//или с новой копией этой формы если человек ушем а затем вернулся
		public void Download(Attachment attachment)
		{
			attachment.IsDownloading = true;
			attachment.Session = Session;
			attachment.Entry = Session.GetSessionImplementation().PersistenceContext.GetEntry(attachment);

			var result = ObservLoad(attachment);
			var disposable = new CompositeDisposable(3);
			disposable.Add(Disposable.Create(() => {
				Bus.SendMessage<Loadable>(attachment, "completed");
			}));
			var progress = result.Item1
				.ObserveOn(UiScheduler)
				.Subscribe(p => attachment.Progress =  p.EventArgs.ProgressPercentage / 100d);
			disposable.Add(progress);

			var download = result.Item2
				.ObserveOn(UiScheduler)
				.Subscribe(_ => {
						var notification = "";
						SessionGaurd(attachment.Session, attachment, (s, a) => {
							var record = a.UpdateLocalFile(Shell.Config);
							s.Save(record);
							Bus.SendMessage(record);
							notification = String.Format("Файл '{0}' загружен", a.Name);
							if (IsActive)
								ResultsSink.OnNext(Open(a));
						});
						Shell.Notifications.OnNext(notification);
					},
					_ => {
						attachment.RequstCancellation.Dispose();
						attachment.IsError = true;
					},
					() => {
						attachment.RequstCancellation.Dispose();
					});
			disposable.Add(download);
			attachment.RequstCancellation = disposable;
			Bus.SendMessage<Loadable>(attachment);
		}

		private void SessionGaurd<T>(ISession session, T entity, Action<ISession, T> action)
		{
			if (session != null && session.IsOpen) {
				action(session, entity);
			}
			else {
				using (var s = Env.Factory.OpenSession())
				using (var t = s.BeginTransaction()) {
					var e = s.Load<T>(Util.GetValue(entity, "Id"));
					action(s, e);
					s.Flush();
					t.Commit();
				}
			}
		}

		public IResult Open(Attachment attachment)
		{
			return new OpenResult(attachment.LocalFilename);
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

		public void Cancel(Attachment attachment)
		{
			attachment.IsDownloading = false;
			attachment.RequstCancellation.Dispose();
		}

		private System.Tuple<IObservable<EventPattern<HttpProgressEventArgs>>, IObservable<string>> ObservLoad(Loadable attachment)
		{
			var version = typeof(AppBootstrapper).Assembly.GetName().Version;
			var handler = new HttpClientHandler {
				Credentials = Settings.Value.GetCredential(),
				PreAuthenticate = true,
				Proxy = Settings.Value.GetProxy()
			};
			if (handler.Credentials == null)
				handler.UseDefaultCredentials = true;
			var progress = new ProgressMessageHandler();
			var handlers = Settings.Value.Handlers().Concat(new [] { progress }).ToArray();
			var client = HttpClientFactory.Create(handler, handlers);
			client.DefaultRequestHeaders.Add("version", version.ToString());
			if (Settings.Value.DebugTimeout > 0)
				client.DefaultRequestHeaders.Add("debug-timeout", Settings.Value.DebugTimeout.ToString());
			if (Settings.Value.DebugFault)
				client.DefaultRequestHeaders.Add("debug-fault", "true");
			client.BaseAddress = Shell.Config.BaseUrl;

			var data = new [] {
				String.Format("urn:data:{0}:{1}", attachment.GetType().Name.ToLower(), attachment.GetId())
			};

			//review - я не понимаю как это может быть но если сделать dispose у observable
			//то ожидающий запрос тоже будет отменен и cancellationtoke не нужен
			//очевидного способа как это может работать нет но как то оно работает
			var result = Tuple.Create(
				Observable.FromEventPattern<HttpProgressEventArgs>(progress, "HttpReceiveProgress"),
				Observable
					.Using(() => client, c => c.PostAsJsonAsync("Download", data).ToObservable())
					.Do(r => r.EnsureSuccessStatusCode())
					.Select(r => r.Content.ReadAsStreamAsync())
					.SubscribeOn(Scheduler)
					.Select(s => Extract(s, attachment.GetLocalFilename(Shell.Config)))
			);
			return Env.WrapRequest(result);
		}

		private string Extract(Task<Stream> task, string name)
		{
			var dir = Path.GetDirectoryName(name);
			if (!Directory.Exists(dir))
				FileHelper.CreateDirectoryRecursive(dir);

			using (var zip = ZipFile.Read(task.Result)) {
				var file = zip.First();
				using (var target = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.None)) {
					file.Extract(target);
				}
			}
			return name;
		}
	}
}