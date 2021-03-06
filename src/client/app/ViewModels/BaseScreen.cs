﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Interactivity;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
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
		private Dictionary<string, List<ColumnSettings>> temporaryTableSettings
			= new Dictionary<string, List<ColumnSettings>>();

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
			RestoreSettingWithReopenScreen();
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

			if (!close) {
				SaveSettingWithReopenScreen();
			}

			if (broacast)
				Broadcast();
		}

		protected IEnumerable<DataGrid> GetControls(object view)
		{
			var dependencyObject = view as DependencyObject;
			if (dependencyObject == null)
				return Enumerable.Empty<DataGrid>();
			return dependencyObject.LogicalDescendants().OfType<DataGrid>()
				.Where(c => Interaction.GetBehaviors(c).OfType<Persistable>().Any());
		}
		protected IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null) {
				yield return new DialogResult(new SimpleSettings(docSettings));
			}

			var printResult = new PrintResult(name, doc);
			var model = new PrintPreviewViewModel(printResult);
			yield return new DialogResult(model, fullScreen: true);
		}

		private void SaveSettingWithReopenScreen()
		{
			foreach (var grid in GetControls(GetView())) {
				var key = grid.Name;
				if (temporaryTableSettings.ContainsKey(key)) {
					temporaryTableSettings.Remove(key);
				}
				temporaryTableSettings.Add(key, grid.Columns.Select((c, i) => new ColumnSettings(grid, c, i)).ToList());
			}
		}

		private void RestoreSettingWithReopenScreen()
		{
			if (temporaryTableSettings.Count == 0)
				return;
			foreach (var grid in GetControls(GetView())) {
				RestoreView(grid, temporaryTableSettings);
			}
		}
		private void RestoreView(DataGrid dataGrid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var settings = storage.GetValueOrDefault(dataGrid.Name);
			if (settings == null)
				return;
			foreach (var column in settings.OrderBy(c => c.DisplayIndex))
				column.Restore(dataGrid, dataGrid.Columns);
			var sorted = settings.FirstOrDefault(x => x.SortDirection != null);
			if (sorted != null) {
				var column = DataGridHelper.FindColumn(dataGrid, sorted.Name);
				if (column != null) {
					foreach (var gridColumn in dataGrid.Columns)
						gridColumn.SortDirection = null;
					column.SortDirection = sorted.SortDirection;
					dataGrid.Items.SortDescriptions.Clear();
					dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, column.SortDirection.Value));
				}
			}
			foreach (var column in dataGrid.Columns.Where(c => !settings.Select(s => s.Name).Contains(DataGridHelper.GetHeader(c)))) {
				column.DisplayIndex = dataGrid.Columns.IndexOf(column);
			}
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
			//Разрешается переходить назад только из дочерних окон
			if (Shell?.Navigator?.NavigationStack.Count() > 0) Shell.Navigator.NavigateBack();
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
			Env.Query(s => s.Update(sender)).LogResult();
		}

		protected void WatchForUpdate<T>(IObservable<T> value)
		{
			value.ChangedValue().Subscribe(x => Env.Query(s => s.Update(x.Sender)).LogResult());
		}

		public override string ToString()
		{
			return String.IsNullOrEmpty(DisplayName) ? base.ToString() : DisplayName;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			OnCloseDisposable.Dispose();
			//если у нас есть активная транакция значит комит этой транакции завершился ошибкой
			if (Session?.Transaction.IsActive == true)
				Session.Transaction.Rollback();
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

		public virtual void OpenLink(string url)
		{
			Process.Start(new ProcessStartInfo(url));
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
			var styles = Env.Query(s => s.Query<CustomStyle>().ToArray()).Result;
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
			Env.Query(s => s.Update(style)).LogResult();
			StyleHelper.BuildStyles(App.Current.Resources, styles);
			Bus.SendMessage(styles);
		}

#if DEBUG
		public virtual object[] GetRebuildArgs()
		{
			return new object[0];
		}
#endif

		public virtual IResult ConfigureGrid(DataGrid grid)
		{
			return new DialogResult(new GridConfig(grid));
		}

		public virtual IObservable<T> RxQuery<T>(Func<IStatelessSession, T> select)
		{
			return Env.RxQuery(select, CloseCancellation.Token);
		}

		public Task Query(Action<IStatelessSession> action)
		{
			return Env.Query(action, CloseCancellation.Token);
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

		protected bool IsValide(IDataErrorInfo2 mode)
		{
			foreach (var field in mode.FieldsForValidate) {
				var error = mode[field];
				if (!string.IsNullOrEmpty(error)) {
					Manager.Warning(error);
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Обновление представления столбцов пользователем
		/// </summary>
		public virtual void UpdateColumns()
		{
		}
	}
}