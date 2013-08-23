using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.Tools;
using Iesi.Collections;
using NHibernate;
using NHibernate.Impl;
using NHibernate.Linq;
using Newtonsoft.Json;
using ReactiveUI;
using LogManager = log4net.LogManager;
using ILog = log4net.ILog;
using WindowManager = AnalitF.Net.Client.Extentions.WindowManager;

namespace AnalitF.Net.Client.ViewModels
{
	public interface IPrintable
	{
		bool CanPrint { get; }

		PrintResult Print();
	}

	[DataContract]
	public class ShellViewModel : BaseConductor
	{
		private Stack<IScreen> navigationStack = new Stack<IScreen>();
		private WindowManager windowManager;
		private ISession session;
		private IStatelessSession statelessSession;
		private ILog log = LogManager.GetLogger(typeof(ShellViewModel));
		private List<Address> addresses;
		private Address currentAddress;

		protected IScheduler Scheduler = BaseScreen.TestSchuduler ?? DefaultScheduler.Instance;
		protected IScheduler UiScheduler = BaseScreen.TestSchuduler ?? DispatcherScheduler.Current;
		protected IMessageBus Bus = RxApp.MessageBus;

		public bool Quiet;

		public ShellViewModel()
		{
			DisplayName = "АналитФАРМАЦИЯ";

			Stat = new NotifyValue<Stat>(new Stat());
			User = new NotifyValue<User>();
			Settings = new NotifyValue<Settings>();
			ErrorCount = new NotifyValue<int>();
			HaveErrors = new NotifyValue<bool>(() => ErrorCount.Value > 0, ErrorCount);
			Version = typeof(ShellViewModel).Assembly.GetName().Version.ToString();
			Arguments = Environment.GetCommandLineArgs();

			var factory = AppBootstrapper.NHibernate.Factory;
			session = factory.OpenSession();
			statelessSession = factory.OpenStatelessSession();
			windowManager = (WindowManager)IoC.Get<IWindowManager>();

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => UpdateDisplayName());

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => NotifyOfPropertyChange("CanPrint"));

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => NotifyOfPropertyChange("CanExport"));

			this.ObservableForProperty(m => m.CanPrint)
				.Subscribe(_ => NotifyOfPropertyChange("CanPrintPreview"));

			this.ObservableForProperty(m => (object)m.Stat.Value)
				.Merge(this.ObservableForProperty(m => (object)m.CurrentAddress))
				.Subscribe(_ => NotifyOfPropertyChange("CanSendOrders"));

			this.ObservableForProperty(m => m.CurrentAddress)
				.Subscribe(e => UpdateStat());

			this.ObservableForProperty(m => m.Settings.Value)
				.Subscribe(_ => {
					NotifyOfPropertyChange("CanShowCatalog");
					NotifyOfPropertyChange("CanSearchOffers");
					NotifyOfPropertyChange("CanShowMnn");
					NotifyOfPropertyChange("CanShowPrice");

					NotifyOfPropertyChange("CanShowOrders");
					NotifyOfPropertyChange("CanShowOrderLines");

					NotifyOfPropertyChange("CanShowJunkOffers");
					NotifyOfPropertyChange("CanShowRejects");
					NotifyOfPropertyChange("CanShowWaybills");
					NotifyOfPropertyChange("CanMicroUpdate");
				});

			Bus.Listen<Stat>()
				.Subscribe(e => Stat.Value = new Stat(e, Stat.Value));
			NotifyValueHelper.LiveValue(Settings, Bus, UiScheduler, session);
		}

		public string[] Arguments;

		[DataMember]
		public Dictionary<string, List<ColumnSettings>> ViewSettings = new Dictionary<string, List<ColumnSettings>>();

		[DataMember]
		public Dictionary<string, string> ViewModelSettings = new Dictionary<string, string>();

		[DataMember]
		public bool ShowAllAddresses { get; set; }

		public NotifyValue<Settings> Settings { get; set; }
		public NotifyValue<User> User { get; set; }
		public NotifyValue<Stat> Stat { get; set; }

		public NotifyValue<int> ErrorCount { get; set; }
		public NotifyValue<bool> HaveErrors { get; set; }

		public string Version { get; set; }

		public List<Address> Addresses
		{
			get { return addresses; }
			set
			{
				addresses = value;
				NotifyOfPropertyChange("Addresses");
			}
		}

		[DataMember]
		public Address CurrentAddress
		{
			get { return currentAddress; }
			set
			{
				currentAddress = value;
				ResetNavigation();
				NotifyOfPropertyChange("CurrentAddress");
			}
		}

		protected override void OnInitialize()
		{
			Reload();
		}

		public override void OnViewReady()
		{
			var import = Arguments.LastOrDefault().Match("import");
			if (import) {
				Coroutine.BeginExecute(Import().GetEnumerator());
			}
			else {
				StartCheck();
			}
		}

		public override void CanClose(Action<bool> callback)
		{
			if (!Quiet) {

				var orderDays = -Settings.Value.DeleteOrdersOlderThan;
				var orderQuery = statelessSession.Query<SentOrder>()
					.Where(w => w.SentOn < DateTime.Today.AddDays(orderDays));
				if (orderQuery.Any()) {
					var deleteOldOrders = !Settings.Value.ConfirmDeleteOldOrders ||
						Confirm(String.Format("В архиве заказов обнаружены заказы," +
							" сделанные более {0} дней назад. Удалить их?", Settings.Value.DeleteOrdersOlderThan));
					if (deleteOldOrders) {
						var orders = orderQuery.ToArray();
						foreach (var order in orders) {
							statelessSession.Delete(order);
						}
					}
				}

				var waybillDays = -Settings.Value.DeleteWaybillsOlderThan;
				var query = statelessSession.Query<Waybill>()
					.Where(w => w.WriteTime < DateTime.Today.AddDays(waybillDays));
				if (query.Any()) {
					var deleteOldWaybills = !Settings.Value.ConfirmDeleteOldWaybills ||
						Confirm(String.Format("В архиве заказов обнаружены документы (накладные, отказы)," +
							" сделанные более {0} дней назад. Удалить их?",
								Settings.Value.DeleteWaybillsOlderThan));
					if (deleteOldWaybills) {
						var waybills = query.ToArray();
						foreach (var waybill in waybills) {
							waybill.DeleteFiles(Settings);
							statelessSession.Delete(waybill);
						}
					}
				}

				if (Stat.Value.OrdersCount > 0) {
					if (Confirm("Обнаружены неотправленные заказы. Отправить их сейчас?")) {
						SendOrders();
					}
				}
			}
			base.CanClose(callback);
		}

		public void UpdateStat()
		{
			Stat.Value = Models.Stat.Update(session, CurrentAddress);
		}

		public void StartCheck()
		{
			if (!Settings.Value.IsValid)
				CheckSettings();

			Reload();

			if (!Settings.Value.IsValid)
				return;

			var request = Settings.Value.CheckUpdateCondition();
			if (Quiet)
				request = null;

			if (!String.IsNullOrEmpty(request) && Confirm(request))
				Update();
		}

		private bool CheckSettings()
		{
			Reload();
			if (!Settings.Value.IsValid) {
				windowManager.Warning("Для начала работы с программой необходимо заполнить учетные данные");
				ShowSettings();
				return false;
			}
			return true;
		}

		public void Reload()
		{
			session.Clear();

			//нужно сохранить идентификатор выбранного адреса доставки тк
			//строка Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			//сбросит его
			var addressId = CurrentAddress == null ? 0u : CurrentAddress.Id;

			Settings.Value = session.Query<Settings>().First();
			User.Value = session.Query<User>().FirstOrDefault();
			Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.Where(a => a.Id == addressId)
				.DefaultIfEmpty(Addresses.FirstOrDefault())
				.FirstOrDefault();
		}

		protected void UpdateDisplayName()
		{
			var value = "АналитФАРМАЦИЯ";
			var named =  ActiveItem as IHaveDisplayName;

			if (named != null && !String.IsNullOrEmpty(named.DisplayName)) {
				value += " - " + named.DisplayName;
			}
			DisplayName = value;
		}

		public bool CanExport
		{
			get
			{
				var exportable = ActiveItem as IExportable;
				if (exportable != null) {
					return exportable.CanExport;
				}
				return false;
			}
		}

		public IResult Export()
		{
			if (!CanExport)
				return null;

			return ((IExportable)ActiveItem).Export();
		}

		public bool CanPrint
		{
			get
			{
				var printable = ActiveItem as IPrintable;
				if (printable != null) {
					return printable.CanPrint;
				}
				return false;
			}
		}

		public IResult Print()
		{
			if (!CanPrint)
				return null;

			return ((IPrintable)ActiveItem).Print();
		}

		public bool CanPrintPreview
		{
			get { return CanPrint; }
		}

		public void PrintPreview()
		{
			if (!CanPrintPreview)
				return;

			var printResult = ((IPrintable)ActiveItem).Print();
			windowManager.ShowDialog(new PrintPreviewViewModel(printResult));
		}

		public void ShowAbout()
		{
			windowManager.ShowFixedDialog(new About());
		}

		public bool CanShowCatalog
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowCatalog()
		{
			ActivateRootItem(new CatalogViewModel());
		}

		public bool CanShowPrice
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowPrice()
		{
			var model = new PriceViewModel {
				OpenSinglePrice = true
			};
			ActivateRootItem(model);
		}

		public bool CanShowMnn
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowMnn()
		{
			ActivateRootItem(new MnnViewModel());
		}

		public bool CanSearchOffers
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void SearchOffers()
		{
			ActivateRootItem(new SearchOfferViewModel());
		}

		public void ShowSettings()
		{
			windowManager.ShowFixedDialog(new SettingsViewModel());
		}

		public bool CanShowOrderLines
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowOrderLines()
		{
			ActivateRootItem(new OrderLinesViewModel());
		}

		public bool CanShowJunkOffers
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowJunkOffers()
		{
			ActivateRootItem(new JunkOfferViewModel());
		}

		public bool CanShowRejects
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowRejects()
		{
			ActivateRootItem(new RejectsViewModel());
		}

		public bool CanShowOrders
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowOrders()
		{
			if (ActiveItem is CatalogOfferViewModel)
				Navigate(new OrdersViewModel());
			else
				ActivateRootItem(new OrdersViewModel());
		}

		public bool CanShowWaybills
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowWaybills()
		{
			ActivateRootItem(new WaybillsViewModel());
		}

		private void ActivateRootItem(IScreen screen)
		{
			if (ActiveItem != null && ActiveItem.GetType() == screen.GetType())
				return;

			while (navigationStack.Count > 0) {
				var closing = navigationStack.Peek();
				if (closing.GetType() == screen.GetType())
					break;
				navigationStack.Pop();
				closing.TryClose();
			}

			if (ActiveItem != null)
				ActiveItem.TryClose();

			if (ActiveItem == null)
				ActivateItem(screen);
		}

		public bool CanMicroUpdate
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public IEnumerable<IResult> MicroUpdate()
		{
			return Sync(new UpdateCommand(Tasks.ArchiveFile, Tasks.ExtractPath, Tasks.RootPath) {
				SyncData = new [] {"Waybills"}
			});
		}

		public IEnumerable<IResult> Update()
		{
			return Sync(new UpdateCommand(Tasks.ArchiveFile, Tasks.ExtractPath, Tasks.RootPath));
		}

		public bool CanSendOrders
		{
			get
			{
				return Stat.Value.ReadyForSendOrdersCount > 0 && CurrentAddress != null;
			}
		}

		public IEnumerable<IResult> SendOrders()
		{
			if (Settings.Value.ConfirmSendOrders && !Confirm("Вы действительно хотите отправить заказы?"))
				return Enumerable.Empty<IResult>();
			return Sync(new SendOrders(CurrentAddress));
		}

		private IEnumerable<IResult> Import()
		{
			return Sync("Обновление завершено успешно.",
				"Не удалось получить обновление. Попробуйте повторить операцию позднее.",
				null,
				Tasks.Import);
		}

		public void CheckDb()
		{
			RunTask(
				new WaitViewModel("Производится восстановление базы данных.\r\nПодождите..."),
				token => {
					var command = new RepairDb();
					command.Token = token;
					command.Execute();
					return command.Result;
				},
				t => {
					if (t.Result) {
						windowManager.Notify("Проверка базы данных завершена.\r\nОшибок не найдено.");
					}
					else {
						var result = windowManager.Question("Восстановление базы данных завершено.\r\n"
							+ "В результате восстановления некоторые данные могли быть потеряны и необходимо сделать кумулятивное обновление.\r\n"
							+ "Выполнить кумулятивное обновление?");
						if (result == MessageBoxResult.Yes)
							Update();
					}
				});
			Reload();
		}

		public void CleanDb()
		{
			if (!Confirm("При создании базы данных будут потеряны текущие заказы.\r\nПродолжить?"))
				return;

			RunTask(
				new WaitViewModel("Производится очистка базы данных.\r\nПодождите..."),
				token => {
					var command = new CleanDb();
					command.Token = token;
					command.Execute();
					return command.Result;
				},
				t => Update());
			Reload();
		}

		private bool Confirm(string text)
		{
			var result = windowManager.Question(text);
			return result == MessageBoxResult.Yes;
		}

		public IEnumerable<IResult> Sync(RemoteCommand command)
		{
			if(UnitTesting) {
				command = OnCommandExecuting(command);
			}

			return Sync(command.SuccessMessage,
				command.ErrorMessage,
				command,
				(c, t, p) => {
					command.BaseUri = Tasks.BaseUri;
					command.Credentials = c;
					command.Token = t;
					command.Progress = p;
					return command.Run();
				});
		}

		private IEnumerable<IResult> Sync(string sucessMessage,
			string errorMessage,
			RemoteCommand command,
			Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, UpdateResult> func)
		{
			if (!CheckSettings())
				return Enumerable.Empty<IResult>();

			var progress = new BehaviorSubject<Progress>(new Progress());
			var wait = new SyncViewModel(progress) {
				GenericErrorMessage = errorMessage
			};
			var credential = new NetworkCredential(Settings.Value.UserName, Settings.Value.Password);

			var results = new IResult[0];
			RunTask(wait,
				t => func(credential, t, progress),
				t => {
					if (t.Result == UpdateResult.UpdatePending) {
						RunUpdate();
					}
					else {
						windowManager.Notify(sucessMessage);
						if (command != null)
							results = command.Results.ToArray();
					}
				});
			Reload();
			return results;
		}

		private void RunUpdate()
		{
			windowManager.Warning("Получена новая версия программы. Сейчас будет выполнено обновление.");
			var updateExePath = Path.Combine(Tasks.ExtractPath, "update", "Updater.exe");
			StartProcess(updateExePath, Process.GetCurrentProcess().Id.ToString());
			//не нужно ничего запрашивать нужно просто выйти
			Quiet = true;
			TryClose();
		}

		private void RunTask<T>(WaitViewModel viewModel, Func<CancellationToken, T> func, Action<Task<T>> success)
		{
			ResetNavigation();

			bool done;
			int count = 0;
			do {
				count++;
				done = true;
				TaskScheduler scheduler;
				if (SynchronizationContext.Current != null)
					scheduler = TaskScheduler.FromCurrentSynchronizationContext();
				else
					scheduler = TaskScheduler.Current;

				//если это вторая итерация то нужно пересодать cancellation
				//тк у предыдущего уже будет стоять флаг IsCancellationRequested
				//и ничего не запустится
				viewModel.Cancellation = new CancellationTokenSource();
				var token = viewModel.Cancellation.Token;
				var task = new Task<T>(() => func(token), token);
				task.ContinueWith(t => {
					viewModel.IsCompleted = true;
					viewModel.TryClose();
				}, scheduler);
				task.Start();

				windowManager.ShowFixedDialog(viewModel);

				if (!task.IsCanceled && !task.IsFaulted) {
					success(task);
				}
				else if (task.IsFaulted) {
					var baseException = task.Exception.GetBaseException();
					if (baseException is TaskCanceledException)
						return;
					log.Error(task.Exception);

					var error = ErrorHelper.TranslateException(task.Exception)
						?? viewModel.GenericErrorMessage;
					windowManager.Error(error);

					//показывать форму с настройками нужно только один раз
					if (count == 1
						&& baseException is RequestException
						&& ((RequestException)baseException).StatusCode == HttpStatusCode.Unauthorized) {
						var model = new SettingsViewModel();
						model.SelectedTab.Value = "LoginTab";
						windowManager.ShowFixedDialog(model);
						done = !model.IsCredentialsChanged;
					}
				}
			} while (!done);
		}

		public void Navigate(IScreen item)
		{
			if (ActiveItem != null) {
				navigationStack.Push(ActiveItem);
				DeactivateItem(ActiveItem, false);
			}

			ActivateItem(item);
		}

		public IEnumerable<IScreen> NavigationStack
		{
			get { return navigationStack; }
		}

		public void ResetNavigation()
		{
			while (navigationStack.Count > 0) {
				var screen = navigationStack.Pop();
				screen.TryClose();
			}

			if (ActiveItem != null)
				ActiveItem.TryClose();
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			ResetNavigation();

			var chain = views.TakeWhile((s, i) => i < views.Length - 1);
			foreach (var screen in chain) {
				navigationStack.Push(screen);
			}
			ActivateItem(views.Last());
		}

		public override void DeactivateItem(IScreen item, bool close)
		{
			base.DeactivateItem(item, close);

			if (close) {
				if (ActiveItem == null && navigationStack.Count > 0) {
					ActivateItem(navigationStack.Pop());
				}
			}
		}

		protected override void OnActivationProcessed(IScreen item, bool success)
		{
			var screen = item as BaseScreen;
			if (screen != null) {
				screen.PostActivated();
			}
		}

#if DEBUG
		public void Collect()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		public void Snoop()
		{
			var assembly = Assembly.LoadFrom(@"C:\Chocolatey\lib\snoop.2.7.1\tools\snoop.exe");
			var type = assembly.GetType("Snoop.SnoopUI");
			type.GetMethod("GoBabyGo", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}
#endif
	}
}