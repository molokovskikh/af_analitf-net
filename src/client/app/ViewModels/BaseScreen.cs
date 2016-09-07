using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
using System.Windows.Input;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
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
	public class QueueScheduler : TaskScheduler, IDisposable
	{
		private BlockingCollection<Task> tasks = new BlockingCollection<Task>(new ConcurrentQueue<Task>());
		private CancellationTokenSource source = new CancellationTokenSource();

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
			while (!source.Token.IsCancellationRequested) {
				foreach (var task in tasks.GetConsumingEnumerable(source.Token)) {
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

		~QueueScheduler()
		{
			Dispose();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			source.Cancel();
		}
	}

	public class BaseScreen : Screen, IActivateEx, IExportable, IDisposable
	{
		private List<PersistedValue> persisted = new List<PersistedValue>();
		private List<PersistedValue> session = new List<PersistedValue>();
		private bool clearSession;
		private bool reload;

		protected bool UpdateOnActivate = true;
		//Флаг отвечает за обновление данных на форме после активации
		//если форма отображает только статичные данные, которые не могут быть отредактированы на других формах
		//тогда нужно установить этот флаг что бы избежать лишних обновлений
		protected bool Readonly;
		protected ILog Log;
		protected ExcelExporter ExcelExporter;
		protected SimpleMRUCache Cache = new SimpleMRUCache(10);

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

		//Флаг для оптимизации восстановления состояния таблиц
		public bool SkipRestoreTable;
		public NotifyValue<object> DbReloadToken = new NotifyValue<object>();
		public NotifyValue<object> OrdersReloadToken = new NotifyValue<object>();
		/// <summary>
		/// использовать только через RxQuery,
		/// живет на протяжении жизни всего приложения и будет закрыто при завершении приложения
		/// </summary>
		public static IStatelessSession BackgroundSession;

		public BaseScreen()
		{
			DisplayName = "АналитФАРМАЦИЯ";
			Log = log4net.LogManager.GetLogger(GetType());
			Manager = (WindowManager)IoC.Get<IWindowManager>();
			OnCloseDisposable.Add(CloseCancellation);
			OnCloseDisposable.Add(ResultsSink);
			Settings = new NotifyValue<Settings>();
			Session = Env.Factory?.OpenSession();
			//для myisam это все бутафория
			//нужно что бы nhibernate делал flush перед запросами
			//если транзакции нет он это делать не будет
			Session?.BeginTransaction();
			//в модульных тестах база не должна использоваться
			StatelessSession = Env.Factory?.OpenStatelessSession();
			Load();
			var properties = GetType().GetProperties()
				.Where(p => p.GetCustomAttributes(typeof(ExportAttribute), true).Length > 0)
				.Select(p => p.Name)
				.Where(p => User.CanExport(this, p))
				.ToArray();
			ExcelExporter = new ExcelExporter(this, properties, Path.GetTempPath());
			CanExport = ExcelExporter.CanExport.ToValue();
		}

		//адрес доставки который выбран в ui
		public Address Address { get; set; }
		public Address[] Addresses { get; set; }
		public User User { get; set; }
		public NotifyValue<Settings> Settings { get; set; }
		public bool IsSuccessfulActivated { get; protected set; }
		public NotifyValue<bool> CanExport { get; set; }

		public WindowManager Manager { get; }
		public IMessageBus Bus => Env.Bus;
		public IScheduler UiScheduler => Env.UiScheduler;
		public IScheduler Scheduler => Env.Scheduler;
		public ISession Session;
		public IStatelessSession StatelessSession;

		protected override void OnInitialize()
		{
			Shell = Shell ?? Parent as ShellViewModel;
			if (Shell != null)
				Address = Addresses.FirstOrDefault(x => x.Id == Shell.CurrentAddress.Value?.Id);

			OnCloseDisposable.Add(Bus.Listen<Settings>()
				.ObserveOn(UiScheduler)
				.Select(_ => {
					Session.Evict(Settings.Value);
					return Session.Query<Settings>().First();
				})
				.Subscribe(x => Settings.Value = x));
			//есть два способа изменить настройки цветов Конфигурация -> Настройка легенды
			//или дважды кликнуть на элементе легенды, подписываемся на события в результате этих действий
			OnCloseDisposable.Add(Settings.Subscribe(_ => RefreshStyles()));
			OnCloseDisposable.Add(Bus.Listen<CustomStyle[]>().ObserveOn(UiScheduler).Subscribe(_ => RefreshStyles()));
			//для сообщений типа string используется ImmediateScheduler
			//те вызов произойдет в той же нитке что и SendMessage
			//если делать это как показано выше .ObserveOn(UiScheduler)
			//то вызов произойдет после того как Dispatcher поделает все дела
			//те деактивирует текущую -> активирует сохраненную форму и вызовет OnActivate
			//установка флага произойдет позже нежели вызов для которого этот флаг устанавливается
			Bus.Listen<string>("db")
				.Where(m => m == "Changed")
				.Subscribe(_ => {
					clearSession = true;
					if (!Readonly)
						UpdateOnActivate = true;
				}, CloseCancellation.Token);
			Bus.Listen<string>("db")
				.Where(m => m == "Reload")
				.Subscribe(_ => {
					reload = true;
					clearSession = true;
					UpdateOnActivate = true;
				}, CloseCancellation.Token);

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
			Session?.Clear();
			Load();
		}

		private void Load()
		{
			User = Session?.Query<User>()?.FirstOrDefault()
				?? Env.User ?? new User {
					SupportHours = "будни: с 07:00 до 19:00",
					SupportPhone = "тел.: 473-260-60-00",
				};
			//обновление настроек обрабатывается отдельно, здесь нужно только загрузить объект из сессии
			//что бы избежать ошибок ленивой загрузки
			Settings.Mute(Session?.Query<Settings>()?.FirstOrDefault()
				?? Env.Settings
					?? new Settings());
			Addresses = Session?.Query<Address>()?.OrderBy(x => x.Name).ToArray()
				?? Env.Addresses?.ToArray() ?? new Address[0];
			Address = Addresses.FirstOrDefault(x => x.Id == Shell?.CurrentAddress.Value?.Id);
		}

		protected override void OnActivate()
		{
			//если это не первичная активация и данные в базе были изменены то нужно перезагрузить сессию
			if (clearSession) {
				RecreateSession();
				clearSession = false;
				OrdersReloadToken.OnNext(new object());
			}

			IsSuccessfulActivated = true;

			if (UpdateOnActivate) {
				Update();
				UpdateOnActivate = false;
			}
			if (reload) {
				reload = false;
				DbReloadToken.OnNext(new object());
			}
		}

		protected override void OnDeactivate(bool close)
		{
			Views.Values.OfType<IPersistable>().Each(x => x.Persister.Save());
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

		//метод вызывается если нужно обновить данные на форме
		//это нужно при открытии формы
		//и если форма была деактивирована а затем вновь активирована
		//и данные в базе изменились
		public virtual void Update()
		{
		}

		public virtual void NavigateBackward()
		{
			Shell?.Navigator?.NavigateBack();
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
					Log.Error($"Не удалось прочитать настройки, для {GetType()}", e);
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
			dependencyObject?.Descendants<DataGrid>().Each(g => {
				if (!User.CanExport(this, g.Name)) {
					g.ClipboardCopyMode = DataGridClipboardCopyMode.None;
				}
			});

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
			StatelessSession?.Update(sender);
		}

		protected void WatchForUpdate<T>(IObservable<T> value)
		{
			value.ChangedValue().Subscribe(x => StatelessSession?.Update(x.Sender));
		}

		public override string ToString()
		{
			return String.IsNullOrEmpty(DisplayName) ? base.ToString() : DisplayName;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			OnCloseDisposable.Dispose();
			StatelessSession?.Dispose();
			StatelessSession = null;
			Session?.Dispose();
			Session = null;
		}

		~BaseScreen()
		{
			//если ошибка возникла в конструкторе, например outofmemory
			try {
				Log?.ErrorFormat("Вызван деструктор для {0} {1}", GetType(), GetHashCode());
				Dispose();
			}
			catch(Exception e) {
				Log?.Error($"Ошибка при освобождении объекта {GetType()} {GetHashCode()}", e);
			}
		}

		private System.Tuple<IObservable<EventPattern<HttpProgressEventArgs>>, IObservable<Stream>> ObserveLoad(Loadable loadable)
		{
			ProgressMessageHandler progress = null;
			HttpClientHandler handler = null;
			var client = Settings.Value.GetHttpClient(Shell.Config, ref progress, ref handler);

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

			var result = ObserveLoad(loadable);
			var disposable = new CompositeDisposable(3) {
				Disposable.Create(() => Bus.SendMessage(loadable, "completed"))
			};
			var progress = result.Item1
				.ObserveOn(UiScheduler)
				.Subscribe(p => loadable.Progress =  p.EventArgs.ProgressPercentage / 100d);
			disposable.Add(progress);

			Log.Debug($"Загрузка {loadable}");
			var download = result.Item2
				.ObserveOn(Scheduler)
				.SelectMany(s => Extract(s, urn => loadable.GetLocalFilename(urn, Shell.Config)).ToObservable())
				.ObserveOn(UiScheduler)
				.Subscribe(name => {
						Log.Debug($"Успешно загружен {loadable}");
						var notification = "";
						SessionGaurd(loadable.Session, loadable, (s, a) => {
							var record = a.UpdateLocalFile(name);
							s.Save(record);
							Bus.SendMessage(record);
							notification = $"Файл '{record.Name}' загружен";
							if (IsActive)
								Open(a).ToObservable().CatchSubscribe(r => ResultsSink.OnNext(r));
						});
						Shell.Notifications.OnNext(notification);
					},
					e => {
						Log.Debug($"Ошибка во время загрузки {loadable}", e);
						SessionGaurd(loadable.Session, loadable, (s, a) => a.Error(e));
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
				method?.Invoke(view, null);
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

		public virtual IObservable<T> RxQuery<T>(Func<IStatelessSession, T> select)
		{
			var task = new Task<T>(() => {
				if (Env.Factory == null)
					return default(T);
				if (BackgroundSession == null)
					BackgroundSession = Env.Factory.OpenStatelessSession();
				lock (BackgroundSession)
					return @select(BackgroundSession);
			}, CloseCancellation.Token);
			//в жизни это невозможно, но в тестах мы можем отменить задачу до того как она запустится
			if (!task.IsCanceled)
				task.Start(Env.QueryScheduler);
			//игнорируем отмену задачи, она произойдет если закрыли форму
			return Observable.FromAsync(() => task).Catch<T, TaskCanceledException>(_ => Observable.Empty<T>());
		}

		public Task Query(Action<IStatelessSession> action)
		{
			var task = new Task(() => {
				if (Env.Factory == null)
					return;
				if (BackgroundSession == null)
					BackgroundSession = Env.Factory.OpenStatelessSession();
				lock (BackgroundSession)
					action(BackgroundSession);
			}, CloseCancellation.Token);
			//в жизни это невозможно, но в тестах мы можем отменить задачу до того как она запустится
			if (!task.IsCanceled)
				task.Start(Env.QueryScheduler);
			return task;
		}

		public Task WaitQueryDrain()
		{
			var t = new Task(() => { });
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

		protected void InitFields()
		{
			InitFields(this);
		}

		public static void InitFields(object screen)
		{
			var notifiable = screen.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType
				&& typeof (NotifyValue<>).IsAssignableFrom(x.PropertyType.GetGenericTypeDefinition())
				&& x.CanWrite);
			foreach (var propertyInfo in notifiable)
				if (propertyInfo.GetValue(screen, null) == null)
					propertyInfo.SetValue(screen, Activator.CreateInstance(propertyInfo.PropertyType), null);
		}
	}
}