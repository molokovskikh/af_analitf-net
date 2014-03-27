using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using Newtonsoft.Json;
using ReactiveUI;
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

		public NotifyValue<Settings> Settings;
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
					&& excelExporter.Properties.Any(k => User.CanExport(this, k.Name));
			}
		}

		protected override void OnInitialize()
		{
			Shell = Shell ?? Parent as ShellViewModel;

			OnCloseDisposable.Add(NotifyValueHelper.LiveValue(Settings, Bus, UiScheduler, Session));
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
	}
}