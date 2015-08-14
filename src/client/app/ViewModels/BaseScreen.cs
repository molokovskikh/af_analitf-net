using System;
using System.Collections.Concurrent;
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
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
using Caliburn.Micro;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using Newtonsoft.Json;
using ReactiveUI;
using Address = AnalitF.Net.Client.Models.Address;
using ILog = log4net.ILog;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.ViewModels
{
	/// <summary>
	/// планировщик задач который все задачи выполняет в одной нитке последовательно
	/// нужен тк mysql требует что бы подключение к базе использовала таже нитка что и создала это подключение
	/// иначе приложение будет падать
	/// </summary>
	public class QueueScheduler : TaskScheduler
	{
		private BlockingCollection<Task> tasks = new BlockingCollection<Task>(new ConcurrentQueue<Task>());

		public QueueScheduler()
		{
			var thread = new Thread(Dispatch) { IsBackground = true };
			thread.Start();
		}

		protected override void QueueTask(Task task)
		{
			tasks.Add(task);
		}

		public void Dispatch()
		{
			while (true) {
				foreach (var task in tasks.GetConsumingEnumerable()) {
					TryExecuteTask(task);
				}
			}
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return tasks;
		}
	}

	public class PersistedValue
	{
		public object DefaultValue;
		public string Key;
		public Func<object> Getter;
		public Action<object> Setter;

		public static PersistedValue Create<T>(NotifyValue<T> value, string key)
		{
			return new PersistedValue {
				DefaultValue = value.Value,
				Key = key,
				Getter = () => value.Value,
				Setter = v => value.Value = (T)v,
			};
		}
	}

	public class BaseScreen : Screen, IActivateEx, IExportable, IDisposable
	{
		private List<PersistedValue> persisted = new List<PersistedValue>();
		private List<PersistedValue> session = new List<PersistedValue>();
		private bool clearSession;

		protected bool UpdateOnActivate = true;
		//Флаг отвечает за обновление данных на форме после активации
		//если форма отображает только статичные данные, которые не могут быть отредактированы на других формах
		//тогда нужно установить этот флаг что бы избежать лишних обновлений
		protected bool Readonly;
		protected ILog Log;
		protected ExcelExporter ExcelExporter;
		protected SimpleMRUCache Cache = new SimpleMRUCache(10);

		protected ISession Session;
		public IStatelessSession StatelessSession;

		public TableSettings TableSettings = new TableSettings();

		//освобождает ресурсы при закрытии формы
		public CompositeDisposable OnCloseDisposable = new CompositeDisposable();
		//сигнал который подается при закрытии формы, может быть использован для отмены операций который выполняются в фоне
		//например web запросов
		public CancellationDisposable CloseCancellation = new CancellationDisposable();

		public ShellViewModel Shell;
		//источник результатов обработки данных которая производится в фоне
		//в тестах на него можно подписаться и проверить что были обработаны нужные результаты
		//в приложении его обрабатывает Coroutine см Config/Initializers/Caliburn
		public Subject<IResult> ResultsSink = new Subject<IResult>();
		public Env Env = Env.Current ?? new Env();

		//todo сессии для фоновых задач здесь не место нужно перенести ее в планировщик
		public ManualResetEventSlim Drained = new ManualResetEventSlim(true);
		private int backgrountQueryCount;

		//адрес доставки который выбран в ui
		public Address Address;
		public Address[] Addresses = new Address[0];

		//Флаг для оптимизации восстановления состояния таблиц
		public bool SkipRestoreTable;
		/// <summary>
		/// использовать только через RxQuery,
		/// живет на протяжении жизни всего приложения и будет закрыто при завершении приложения
		/// </summary>
		private static IStatelessSession _backgroundSession;

		public BaseScreen()
		{
			DisplayName = "АналитФАРМАЦИЯ";
			Log = log4net.LogManager.GetLogger(GetType());
			Manager = (WindowManager)IoC.Get<IWindowManager>();
			OnCloseDisposable.Add(CloseCancellation);
			OnCloseDisposable.Add(ResultsSink);
			Settings = new NotifyValue<Settings>();

			//в юнит тестах база не должна использоваться
			if (Env.Factory != null) {
				StatelessSession = Env.Factory.OpenStatelessSession();
				Session = Env.Factory.OpenSession();
				//для mysql это все бутафория
				//нужно что бы nhibernate делал flush перед запросами
				//если транзакции нет он это делать не будет
				Session.BeginTransaction();

				Settings.Value = Session.Query<Settings>().FirstOrDefault()
					?? new Settings(defaults: true);
				User = Session.Query<User>().FirstOrDefault()
					?? new User {
						SupportHours = "будни: с 07:00 до 19:00",
						SupportPhone = "тел.: 473-260-60-00",
					};
			}
			else {
				Settings.Value = new Settings(defaults: true);
				User = Env.User ?? new User {
					SupportHours = "будни: с 07:00 до 19:00",
					SupportPhone = "тел.: 473-260-60-00",
				};
			}

			var properties = GetType().GetProperties()
				.Where(p => p.GetCustomAttributes(typeof(ExportAttribute), true).Length > 0)
				.Select(p => p.Name)
				.Where(p => User.CanExport(this, p))
				.ToArray();
			ExcelExporter = new ExcelExporter(this, properties, Path.GetTempPath());
			CanExport = ExcelExporter.CanExport.ToValue();
		}

		public User User { get; set; }
		public NotifyValue<Settings> Settings { get; private set; }
		public bool IsSuccessfulActivated { get; protected set; }
		public NotifyValue<bool> CanExport { get; set; }

		public WindowManager Manager { get; private set; }

		public IMessageBus Bus
		{
			get { return Env.Bus; }
		}

		public IScheduler UiScheduler
		{
			get { return Env.UiScheduler; }
		}

		public IScheduler Scheduler
		{
			get { return Env.Scheduler; }
		}

		protected override void OnInitialize()
		{
			Shell = Shell ?? Parent as ShellViewModel;

			OnCloseDisposable.Add(NotifyValueHelper.LiveValue(Settings, Bus, UiScheduler, Session));
			//есть два способа изменить настройки цветов Конфигурация -> Настройка легенды
			//или дважды кликнуть на элементе легенды, подписываемся на события в результате этих действий
			OnCloseDisposable.Add(Settings.Subscribe(_ => RefreshStyles()));
			OnCloseDisposable.Add(Bus.Listen<CustomStyle[]>().ObserveOn(UiScheduler).Subscribe(_ => RefreshStyles()));
			if (!Readonly) {
				//для сообщений типа string используется ImmediateScheduler
				//те вызов произойдет в той же нитке что и SendMessage
				//если делать это как показано выше .ObserveOn(UiScheduler)
				//то вызов произойдет после того как Dispatcher поделает все дела
				//те деактивирует текущую -> активирует сохраненную форму и вызовет OnActivate
				//установка флага произойдет позже нежели вызов для которого этот флаг устанавливается
				Bus.Listen<string>("db")
					.Where(m => m == "Changed")
					.Subscribe(_ => {
						UpdateOnActivate = true;
					}, CloseCancellation.Token);
			}
			Bus.Listen<string>("db")
				.Where(m => m == "Changed")
				.Subscribe(_ => {
					clearSession = true;
				}, CloseCancellation.Token);

			Load();
			Restore();

			if (Shell != null) {
				TableSettings.Persisted = Shell.ViewSettings;
				TableSettings.Prefix = GetType().Name + ".";
				ExcelExporter.ExportDir = Shell.Config.TmpDir;
				try {
					foreach (var value in persisted)
						value.Setter(Shell.GetPersistedValue(value.Key, value.DefaultValue));

					foreach (var value in session)
						value.Setter(Shell.SessionContext.GetValueOrDefault(value.Key, value.DefaultValue));
				}
				catch(Exception e) {

	#if DEBUG
					throw;
	#else
					Log.Error("Не удалось восстановить состояние", e);
	#endif
				}
			}
		}

		//метод нужен для того что бы форма могла изменять
		//ActiveItem тк делать это в OnActivate нельзя
		//например открыть дочерний элемент если он один
		public virtual void PostActivated()
		{
		}

		protected virtual void RecreateSession()
		{
			Session.Clear();
			Load();
			User = Session.Query<User>().FirstOrDefault();
			//обновление настроек обрабатывается отдельно, здесь нужно только загрузить объект из сессии
			//что бы избежать ошибок ленивой загрузки
			Settings.Mute(Session.Query<Settings>().FirstOrDefault(new Settings(true)));
		}

		protected override void OnActivate()
		{
			//если это не первичная активация и данные в базе были изменены то нужно перезагрузить сессию
			if (clearSession) {
				RecreateSession();
				clearSession = false;
			}

			IsSuccessfulActivated = true;

			if (UpdateOnActivate) {
				Update();
				UpdateOnActivate = false;
			}
		}

		protected override void OnDeactivate(bool close)
		{
			foreach (var value in persisted)
				Shell.PersistentContext[value.Key] = value.Getter();

			foreach (var value in session) {
				if (value.DefaultValue != value.Getter())
					Shell.SessionContext[value.Key] = value.Getter();
				else if (Shell.SessionContext.ContainsKey(value.Key))
					Shell.SessionContext.Remove(value.Key);
			}


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
				TableSettings.SaveView(GetView());
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
			if (Session == null)
				return;
			if (Shell != null && Shell.CurrentAddress != null)
				Address = Session.Load<Address>(Shell.CurrentAddress.Id);
			Addresses = Session.Query<Address>().OrderBy(x => x.Name).ToArray();
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
					Log.Error(String.Format("Не удалось прочитать настройки, для {0}", GetType()), e);
				}
				finally {
					IsNotifying = true;
					Shell.ViewModelSettings.Remove(key);
				}
			}
		}

		public void ResetView(DataGrid grid)
		{
			TableSettings.Reset(grid);
		}

		protected override void OnViewLoaded(object view)
		{
			var dependencyObject = view as DependencyObject;
			if (dependencyObject != null) {
				dependencyObject.Descendants<DataGrid>().Each(g => {
					if (!User.CanExport(this, g.Name)) {
						g.ClipboardCopyMode = DataGridClipboardCopyMode.None;
					}
				});
			}

			if (!SkipRestoreTable)
				TableSettings.RestoreView(view);
		}

		//для тестов
		public void SaveDefaults(object view)
		{
			TableSettings.RestoreView(view);
		}

		public virtual IResult Export()
		{
			return ExcelExporter.Export();
		}

		protected bool Confirm(string message)
		{
			return Manager.Question(message) == MessageBoxResult.Yes;
		}

		protected static void Attach(object view, CommandBinding[] commands)
		{
			var el = view as UIElement;
			if (el == null)
				return;

			foreach (var binding in commands) {
				el.CommandBindings.Add(binding);
				var command = binding.Command as RoutedUICommand;
				if (command == null)
					continue;

				foreach (InputGesture o in command.InputGestures) {
					el.InputBindings.Add(new InputBinding(command, o));
				}
			}
		}

		protected void WatchForUpdate(object sender, PropertyChangedEventArgs e)
		{
			StatelessSession.Update(sender);
		}

		protected void WatchForUpdate<T>(NotifyValue<T> currentReject)
		{
			currentReject.ChangedValue()
				.Subscribe(e => WatchForUpdate(e.Sender, e.EventArgs));
		}

		public override string ToString()
		{
			return String.IsNullOrEmpty(DisplayName) ? base.ToString() : DisplayName;
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
			//если ошибка возникла в конструкторе, например outofmemory
			if (Log == null)
				return;
			try {
				Log.ErrorFormat("Вызван деструктор для {0} {1}", GetType(), GetHashCode());
				Dispose();
			}
			catch(Exception e) {
				Log.Error(String.Format("Ошибка при освобождении объекта {0} {1}", GetType(), GetHashCode()), e);
			}
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

		private void RefreshStyles()
		{
			foreach (var view in Views.Values) {
				var method = view.GetType().GetMethod("ApplyStyles");
				if (method != null)
					method.Invoke(view, null);
			}
		}

		public IEnumerable<IResult> EditLegend(string name)
		{
			var styles = StatelessSession.Query<CustomStyle>().ToArray();
			var style = styles.FirstOrDefault(s => s.Name == name);
			if (style == null)
				yield break;
			var isDirty = false;
			style.PropertyChanged += (sender, args) => {
				isDirty = true;
			};
			foreach(var result in CustomStyle.Edit(style))
				yield return result;
			if (!isDirty)
				yield break;
			StatelessSession.Update(style);
			StyleHelper.BuildStyles(App.Current.Resources, styles);
			Bus.SendMessage(styles);
		}

#if DEBUG
		public virtual object[] GetRebuildArgs()
		{
			return new object[0];
		}
#endif

		public IResult ConfigureGrid(DataGrid grid)
		{
			return new DialogResult(new GridConfig(grid));
		}

		public IObservable<T> RxQuery<T>(Func<IStatelessSession, T> select)
		{
			var task = new Task<T>(() => {
				if (_backgroundSession == null)
					_backgroundSession = Env.Factory.OpenStatelessSession();
				Interlocked.Increment(ref backgrountQueryCount);
				try{
					Drained.Reset();
					if (_backgroundSession == null)
						return default(T);
					lock (_backgroundSession) {
						return @select(_backgroundSession);
					}
				}
				finally {
					var val = Interlocked.Decrement(ref backgrountQueryCount);
					if (val == 0)
						Drained.Set();
				}
			}, CloseCancellation.Token);
			task.Start(Env.QueryScheduler);
			return Observable.FromAsync(() => task);
		}

		public Task TplQuery(Action<IStatelessSession> action)
		{
			var task = new Task(() => {
				if (_backgroundSession == null)
					_backgroundSession = Env.Factory.OpenStatelessSession();
				Interlocked.Increment(ref backgrountQueryCount);
				try{
					Drained.Reset();
					if (_backgroundSession == null)
						return;
					lock (_backgroundSession) {
						action(_backgroundSession);
					}
				}
				finally {
					var val = Interlocked.Decrement(ref backgrountQueryCount);
					if (val == 0)
						Drained.Set();
				}
			}, CloseCancellation.Token);
			task.Start(Env.QueryScheduler);
			return task;
		}

		public Task WaitQueryDrain()
		{
			var t = new Task(() => Drained.Wait());
			t.Start(Env.QueryScheduler);
			return t;
		}

		public void Persist<T>(NotifyValue<T> value, string key)
		{
			persisted.Add(PersistedValue.Create(value, key));
		}

		public void SessionValue<T>(NotifyValue<T> value, string key)
		{
			session.Add(PersistedValue.Create(value, key));
		}

		public IList<T> GetItemsFromView<T>(string name)
		{
			var view = GetView();
			if (view == null)
				return null;
			return ((FrameworkElement)view).Descendants<DataGrid>()
				.First(g => g.Name == name)
				.Items
				.OfType<T>().ToArray();
		}
	}
}