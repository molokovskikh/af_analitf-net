using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
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
		private bool updateOnActivate = true;

		protected ILog log;
		protected ExcelExporter excelExporter;
		protected ISession Session;
		protected IStatelessSession StatelessSession;

		protected Address Address;
		protected IMessageBus Bus = RxApp.MessageBus;
		protected CompositeDisposable OnCloseDisposable = new CompositeDisposable();

		public static bool UnitTesting;
		public static IScheduler TestSchuduler;

		public NotifyValue<Settings> Settings;
		public Extentions.WindowManager Manager { get; private set; }
		public IScheduler Scheduler = TestSchuduler ?? DefaultScheduler.Instance;
		public IScheduler UiScheduler = TestSchuduler ?? DispatcherScheduler.Current;

		public BaseScreen()
		{
			DisplayName = "АналитФАРМАЦИЯ";
			log = log4net.LogManager.GetLogger(GetType());
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();

			if (!UnitTesting) {
				var factory = AppBootstrapper.NHibernate.Factory;
				StatelessSession = factory.OpenStatelessSession();
				Session = factory.OpenSession();
				//для mysql это все бутафория
				//нужно что бы nhibernate делал flush перед запросами
				//если транзакции нет он это делать не будет
				Session.BeginTransaction();

				Settings = new NotifyValue<Settings>(Session.Query<Settings>().First());
				User = Session.Query<User>().FirstOrDefault();
			}
			else {
				Settings = new NotifyValue<Settings>(new Settings());
			}

			excelExporter = new ExcelExporter(this);
			OnCloseDisposable.Add(NotifyValueHelper.LiveValue(Settings, Bus, UiScheduler, Session));

			//для сообщений типа string используется ImmediateScheduler
			//те вызов произойдет в той же нитке что и SendMessage
			//если делать это как показано выше .ObserveOn(UiScheduler)
			//то вызов произойдет после того как Dispatcher поделает все дела
			//те деактивирует текущую -> активирует сохраненную форму и вызовет OnActivate
			//установка флага произойдет позже нежели вызов для которого этот флаг устанавливается
			OnCloseDisposable.Add(Bus.Listen<string>()
				.Where(m => m == "DbChanged")
				.Subscribe(_ => updateOnActivate = true));
		}

		protected virtual ShellViewModel Shell
		{
			get { return ((ShellViewModel)Parent); }
		}

		public bool IsSuccessfulActivated { get; protected set; }

		public User User { get; set; }

		public bool CanExport
		{
			get { return excelExporter.CanExport; }
		}

		protected override void OnInitialize()
		{
			Load();
			Restore();

			if (Shell != null) {
				tableSettings.Persisted = Shell.ViewSettings;
				tableSettings.Prefix = GetType().Name + ".";
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
				updateOnActivate = false;
				Update();
			}
		}

		protected override void OnDeactivate(bool close)
		{
			if (close)
				OnCloseDisposable.Dispose();

			var broacast = false;
			if (Session.IsOpen) {
				broacast = Session.IsDirty();
				if (Session.FlushMode != FlushMode.Never)
					Session.Flush();

				if (Session.Transaction.IsActive)
					Session.Transaction.Commit();
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
			Bus.SendMessage("DbChanged");
		}

		private void Load()
		{
			if (Shell == null)
				return;

			if (Shell.CurrentAddress != null)
				Address = Session.Load<Address>(Shell.CurrentAddress.Id);
		}

		//метод вызывается если нужно обновить данные на форме
		//это нужно при открытии формы
		//и если форма была деактивирована а затем вновь активирована
		//и данные в базе изменились
		protected virtual void Update()
		{
		}

		public virtual void NavigateBackward()
		{
			var canClose = Shell == null || Shell.NavigationStack.Any();
			if (canClose)
				TryClose();
		}

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

		protected override void OnViewAttached(object view, object context)
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
			if (StatelessSession != null)
				StatelessSession.Dispose();

			if (Session != null)
				Session.Dispose();
		}
	}
}