using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using NPOI.HSSF.UserModel;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using ReactiveUI;
using Address = AnalitF.Net.Client.Models.Address;
using ILog = log4net.ILog;

namespace AnalitF.Net.Client.ViewModels
{
	public class BaseScreen : Screen, IActivateEx, IExportable, IDisposable
	{
		private TableSettings tableSettings = new TableSettings();
		//screen может быть сконструирован не в главном потоке в этом случае DispatcherScheduler.Current
		//будет недоступен по этому делаем его ленивым и вызываем только в OnInitialize и позже
		private Lazy<IScheduler> uiSheduler = new Lazy<IScheduler>(() => TestSchuduler ?? DispatcherScheduler.Current);

		protected bool updateOnActivate = true;

		//Флаг отвечает за обновление данных на форме после активации
		//если форма отображает только статичные данные, которые не могут быть отредактированы на других формах
		//тогда нужно установить этот флаг что бы избежать лишних обновлений
		protected bool Readonly;
		protected ILog log;
		protected ExcelExporter excelExporter;
		protected ISession Session;
		protected IStatelessSession StatelessSession;

		//адрес доставки который выбран в ui
		public Address Address;
		protected IMessageBus Bus = RxApp.MessageBus;
		//освобождает ресурсы при закрытии формы
		public CompositeDisposable OnCloseDisposable = new CompositeDisposable();
		//сигнал который подается при закрытии формы, может быть использован для отмены операций который выполняются в фоне
		//например web запросов
		public CancellationDisposable CloseCancellation = new CancellationDisposable();

		public static bool UnitTesting;
		public static IScheduler TestSchuduler;

		public NotifyValue<Settings> Settings { get; private set; }
		public Extentions.WindowManager Manager { get; private set; }
		public IScheduler Scheduler = TestSchuduler ?? DefaultScheduler.Instance;

		public ShellViewModel Shell;
		//источник результатов обработки данных которая производится в фоне
		//в тестах на него можно подписаться и проверить что были обработаны нужные результаты
		//в приложении его обрабатывает Coroutine см Config/Initializers/Caliburn
		public Subject<IResult> ResultsSink = new Subject<IResult>();
		public Env Env = new Env();

		public BaseScreen()
		{
			DisplayName = "АналитФАРМАЦИЯ";
			log = log4net.LogManager.GetLogger(GetType());
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
			OnCloseDisposable.Add(CloseCancellation);
			OnCloseDisposable.Add(ResultsSink);

			if (!UnitTesting) {
				StatelessSession = Env.Factory.OpenStatelessSession();
				Session = Env.Factory.OpenSession();
				//для mysql это все бутафория
				//нужно что бы nhibernate делал flush перед запросами
				//если транзакции нет он это делать не будет
				Session.BeginTransaction();

				Settings = new NotifyValue<Settings>(Session.Query<Settings>().First());
				User = Session.Query<User>().FirstOrDefault()
					?? new User();
			}
			else {
				Settings = new NotifyValue<Settings>(new Settings(defaults: true));
				User = new User();
			}

			excelExporter = new ExcelExporter(this, Path.GetTempPath());
		}

		public IScheduler UiScheduler
		{
			get { return uiSheduler.Value; }
		}

		public bool IsSuccessfulActivated { get; protected set; }

		public User User { get; set; }

		public virtual bool CanExport
		{
			get
			{
				return excelExporter.CanExport
					&& excelExporter.Properties.Any(k => User.CanExport(GetType(), k.Name));
			}
		}

		protected override void OnInitialize()
		{
			Shell = Shell ?? Parent as ShellViewModel;

			OnCloseDisposable.Add(NotifyValueHelper.LiveValue(Settings, Bus, UiScheduler, Session));
			Settings.Subscribe(_ => {
				foreach (var view in Views.Values) {
					var method = view.GetType().GetMethod("ApplyStyles");
					if (method != null)
						method.Invoke(view, null);
				}
			});
			if (!Readonly) {
				//для сообщений типа string используется ImmediateScheduler
				//те вызов произойдет в той же нитке что и SendMessage
				//если делать это как показано выше .ObserveOn(UiScheduler)
				//то вызов произойдет после того как Dispatcher поделает все дела
				//те деактивирует текущую -> активирует сохраненную форму и вызовет OnActivate
				//установка флага произойдет позже нежели вызов для которого этот флаг устанавливается
				OnCloseDisposable.Add(Bus.Listen<string>("db")
					.Where(m => m == "Changed")
					.Subscribe(_ => updateOnActivate = true));
			}

			Load();
			Restore();

			if (Shell != null) {
				tableSettings.Persisted = Shell.ViewSettings;
				tableSettings.Prefix = GetType().Name + ".";
				excelExporter.ExportDir = Shell.Config.TmpDir;
			}
		}

		//метод нужен для того что бы форма могла изменять
		//ActiveItem тк делать это в OnActivate нельзя
		//например открыть дочерний элемент если он один
		public virtual void PostActivated()
		{
		}

		protected override void OnActivate()
		{
			IsSuccessfulActivated = true;

			if (updateOnActivate) {
				Update();
				updateOnActivate = false;
			}
		}

		protected override void OnDeactivate(bool close)
		{
			if (close)
				OnCloseDisposable.Dispose();

			var broacast = false;
			//в тестах может быть ситуация когда мы дважды освобождаем объект
			if (Session != null) {
				if (Session.IsOpen) {
					if (Session.FlushMode != FlushMode.Never) {
						//IsDirty - приведет к тому что все изменения будут сохранены
						//по этому делаем проверку только если нужно сохранить изменения
						broacast = Session.IsDirty();
						Session.Flush();
					}

					if (Session.Transaction.IsActive)
						Session.Transaction.Commit();
				}
			}

			if (close) {
				Save();
				tableSettings.SaveView(GetView());
				Dispose();
			}

			if (broacast)
				Broadcast();
		}

		protected virtual void Broadcast()
		{
			Bus.SendMessage("Changed", "db");
		}

		private void Load()
		{
			if (Shell == null)
				return;

			if (Shell.CurrentAddress != null && Session != null)
				Address = Session.Load<Address>(Shell.CurrentAddress.Id);
		}

		//метод вызывается если нужно обновить данные на форме
		//это нужно при открытии формы
		//и если форма была деактивирована а затем вновь активирована
		//и данные в базе изменились
		public virtual void Update()
		{
		}

		public virtual void NavigateBackward()
		{
			var canClose = Shell == null || Shell.NavigationStack.Any();
			if (canClose)
				TryClose();
		}

		//todo мы не должны пытаться сериализовать\десериализовать объекты из базы тк это не имеет смысла
		//их нужно загружать
		private void Save()
		{
			if (Shell == null)
				return;

			var type = GetType();
			if (type.GetCustomAttributes(typeof(DataContractAttribute), true).Length == 0)
				return;

			var key = type.FullName;
			if (Shell.ViewModelSettings.ContainsKey(key)) {
				Shell.ViewModelSettings.Remove(key);
			}
			var json = JsonConvert.SerializeObject(this, JsonHelper.SerializerSettings());
			Shell.ViewModelSettings.Add(key, json);
		}

		public void Restore()
		{
			if (Shell == null)
				return;

			var key = GetType().FullName;
			if (Shell.ViewModelSettings.ContainsKey(key)) {
				try {
					IsNotifying = false;
					JsonConvert.PopulateObject(Shell.ViewModelSettings[key], this, JsonHelper.SerializerSettings());
				}
				catch (Exception e) {
					log.Error(String.Format("Не удалось прочитать настройки, для {0}", GetType()), e);
				}
				finally {
					IsNotifying = true;
					Shell.ViewModelSettings.Remove(key);
				}
			}
		}

		public void ResetView(DataGrid grid)
		{
			tableSettings.Reset(grid);
		}

		protected override void OnViewLoaded(object view)
		{
			tableSettings.RestoreView(view);
		}

		//для тестов
		public void SaveDefaults(object view)
		{
			tableSettings.RestoreView(view);
		}

		public IResult Export()
		{
			return excelExporter.Export();
		}

		protected bool Confirm(string message)
		{
			return Manager.Question(message) == MessageBoxResult.Yes;
		}

		protected static void Attach(object view, CommandBinding[] commands)
		{
			var ui = view as UIElement;
			if (ui == null)
				return;

			foreach (var binding in commands) {
				ui.CommandBindings.Add(binding);
				var command = binding.Command as RoutedUICommand;
				if (command == null)
					continue;

				foreach (InputGesture o in command.InputGestures) {
					ui.InputBindings.Add(new InputBinding(command, o));
				}
			}
		}

		protected void WatchForUpdate(object sender, PropertyChangedEventArgs e)
		{
			StatelessSession.Update(sender);
		}

		protected void WatchForUpdate(NotifyValue<Reject> currentReject)
		{
			currentReject.ChangedValue()
				.Subscribe(e => WatchForUpdate(e.Sender, e.EventArgs));
		}

		public override string ToString()
		{
			return DisplayName;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			OnCloseDisposable.Dispose();
			if (StatelessSession != null) {
				StatelessSession.Dispose();
				StatelessSession = null;
			}

			if (Session != null) {
				Session.Dispose();
				Session = null;
			}
		}

		~BaseScreen()
		{
			try {
				log.ErrorFormat("Вызван деструктор для {0} {1}", GetType(), GetHashCode());
				Dispose();
			}
			catch(Exception e) {
				log.Error(String.Format("Ошибка при освобождении объекта {0} {1}", GetType(), GetHashCode()), e);
			}
		}

		protected void ValidateAndClose(IDataErrorInfo2 item)
		{
			foreach (var field in item.FieldsForValidate) {
				var error = item[field];
				if (!string.IsNullOrEmpty(error)) {
					Manager.Warning(error);
					return;
				}
			}
			TryClose(true);
		}

		private System.Tuple<IObservable<EventPattern<HttpProgressEventArgs>>, IObservable<Stream>> ObservLoad(Loadable loadable)
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
			var handlers = Settings.Value.Handlers().Concat(new[] { progress }).ToArray();
			var client = HttpClientFactory.Create(handler, handlers);
			client.DefaultRequestHeaders.Add("version", version.ToString());
			if (Settings.Value.DebugTimeout > 0)
				client.DefaultRequestHeaders.Add("debug-timeout", Settings.Value.DebugTimeout.ToString());
			if (Settings.Value.DebugFault)
				client.DefaultRequestHeaders.Add("debug-fault", "true");
			client.BaseAddress = Shell.Config.BaseUrl;

			var data = new[] {
				String.Format("urn:data:{0}:{1}", NHibernateUtil.GetClass(loadable).Name.ToLower(), loadable.GetId())
			};

			//review - я не понимаю как это может быть но если сделать dispose у observable
			//то ожидающий запрос тоже будет отменен и cancellationtoke не нужен
			//очевидного способа как это может работать нет но как то оно работает
			var result = Tuple.Create(
				Observable.FromEventPattern<HttpProgressEventArgs>(progress, "HttpReceiveProgress"),
				Observable
					.Using(() => client, c => c.PostAsJsonAsync("Download", data).ToObservable())
					.Do(r => r.EnsureSuccessStatusCode())
					.SelectMany(r => r.Content.ReadAsStreamAsync().ToObservable())
				);
			return Env.WrapRequest(result);
		}

		private IEnumerable<string> Extract(Stream stream, Func<string, string> toName)
		{
			using (var zip = ZipFile.Read(stream)) {
				foreach (var entry in zip) {
					var name = toName(entry.FileName);
					var dir = Path.GetDirectoryName(name);
					if (!Directory.Exists(dir))
						FileHelper.CreateDirectoryRecursive(dir);
					if (!String.IsNullOrEmpty(name)) {
						using (var target = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.None)) {
							entry.Extract(target);
						}
						yield return name;
					}
				}
			}
		}

		public void Cancel(Loadable loadable)
		{
			loadable.IsDownloading = false;
			loadable.RequstCancellation.Dispose();
		}

		public virtual IEnumerable<IResult> Open(Loadable loadable)
		{
			return loadable.GetFiles().Select(n => new OpenResult(n));
		}

		//todo - если соединение быстрое то загрузка происходит так быстро что элементы управления мигают
		//черная магия будь бдителен все обработчики живут дольше чем форма и могут быть вызваны после того как форма была закрыта
		//или с новой копией этой формы если человек ушел а затем вернулся
		public virtual IEnumerable<IResult> Download(Loadable loadable)
		{
			loadable.IsDownloading = true;
			loadable.Session = Session;
			loadable.Entry = Session.GetSessionImplementation().PersistenceContext.GetEntry(loadable);

			var result = ObservLoad(loadable);
			var disposable = new CompositeDisposable(3) {
				Disposable.Create(() => Bus.SendMessage(loadable, "completed"))
			};
			var progress = result.Item1
				.ObserveOn(UiScheduler)
				.Subscribe(p => loadable.Progress =  p.EventArgs.ProgressPercentage / 100d);
			disposable.Add(progress);

			var download = result.Item2
				.ObserveOn(Scheduler)
				.SelectMany(s => Extract(s, urn => loadable.GetLocalFilename(urn, Shell.Config)).ToObservable())
				.ObserveOn(UiScheduler)
				.Subscribe(name => {
						var notification = "";
						SessionGaurd(loadable.Session, loadable, (s, a) => {
							var record = a.UpdateLocalFile(name);
							s.Save(record);
							Bus.SendMessage(record);
							notification = String.Format("Файл '{0}' загружен", record.Name);
							if (IsActive)
								Open(a).ToObservable().CatchSubscribe(r => ResultsSink.OnNext(r));
						});
						Shell.Notifications.OnNext(notification);
					},
					e => {
						SessionGaurd(loadable.Session, loadable, (s, a) => a.Error());
					},
					() => {
						//если loadable.IsDownloaded = true то значит запрос дал результаты
						//и состояние сохранено в обработчике он next не нужны обращаться к базе нужно просто освободить ресурсы
						//если ничего не было найдено то нужно открыть сессию что сохранить состояние объекта
						if (loadable.IsDownloaded) {
							loadable.Completed();
						}
						else {
							SessionGaurd(loadable.Session, loadable, (s, a) => a.Completed());
						}
					});
			disposable.Add(download);
			loadable.RequstCancellation = disposable;
			Bus.SendMessage(loadable);
			return Enumerable.Empty<IResult>();
		}

		protected void SessionGaurd<T>(ISession session, T entity, Action<ISession, T> action)
		{
			if (session != null && session.IsOpen) {
				action(session, entity);
			}
			else {
				using (var s = Env.Factory.OpenSession())
				using (var t = s.BeginTransaction()) {
					var e = s.Load(NHibernateUtil.GetClass(entity), Util.GetValue(entity, "Id"));
					action(s, (T)e);
					s.Flush();
					t.Commit();
				}
			}
		}
	}
}