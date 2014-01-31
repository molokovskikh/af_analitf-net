using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using Iesi.Collections;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using Address = AnalitF.Net.Client.Models.Address;
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
	public class ShellViewModel : BaseShell, IDisposable
	{
		private WindowManager windowManager;
		private Config.Config config = new Config.Config();
		private ISession session;
		private IStatelessSession statelessSession;
		private ILog log = LogManager.GetLogger(typeof(ShellViewModel));
		private List<Address> addresses = new List<Address>();
		private Address currentAddress;

		private Main defaultItem;

		protected IScheduler Scheduler = BaseScreen.TestSchuduler ?? DefaultScheduler.Instance;
		protected IScheduler UiScheduler = BaseScreen.TestSchuduler ?? DispatcherScheduler.Current;
		protected IMessageBus Bus = RxApp.MessageBus;

		[DataMember]
		public Dictionary<string, List<ColumnSettings>> ViewSettings = new Dictionary<string, List<ColumnSettings>>();
		[DataMember]
		public Dictionary<string, string> ViewModelSettings = new Dictionary<string, string>();

		public Subject<string> Notifications = new Subject<string>();
		public CompositeDisposable CloseDisposable = new CompositeDisposable();

		public ShellViewModel()
		{
			DisplayName = "АналитФАРМАЦИЯ";
			defaultItem = new Main(Config);
			Navigator.DefaultScreen = defaultItem;

#if DEBUG
			if (!UnitTesting)
				Debug = new Debug();
#endif

			Stat = new NotifyValue<Stat>(new Stat());
			User = new NotifyValue<User>();
			Settings = new NotifyValue<Settings>();
			IsDataLoaded = new NotifyValue<bool>(
				() => Settings.Value != null && Settings.Value.LastUpdate != null,
				Settings);
			Version = typeof(ShellViewModel).Assembly.GetName().Version.ToString();
			NewMailsCount = new NotifyValue<int>();
			PendingDownloads = new ObservableCollection<Loadable>();

			session = Env.Factory.OpenSession();
			statelessSession = Env.Factory.OpenStatelessSession();
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

			CloseDisposable.Add(Bus.Listen<Loadable>().ObserveOn(UiScheduler).Subscribe(l => {
				CloseDisposable.Add(l.RequstCancellation);
				PendingDownloads.Add(l);
			}));
			CloseDisposable.Add(Bus.Listen<Loadable>("completed").ObserveOn(UiScheduler).Subscribe(l => {
				CloseDisposable.Remove(l.RequstCancellation);
				PendingDownloads.Remove(l);
			}));
			CloseDisposable.Add(Bus.Listen<Stat>().Subscribe(e => Stat.Value = new Stat(e, Stat.Value)));
			CloseDisposable.Add(NotifyValueHelper.LiveValue(Settings, Bus, UiScheduler, session));
		}

		public Config.Config Config
		{
			get { return config; }
			set
			{
				config = value;
				defaultItem.Config = value;
			}
		}

		[DataMember]
		public bool ShowAllAddresses { get; set; }

		public ObservableCollection<Loadable> PendingDownloads { get; set; }

		public NotifyValue<Settings> Settings { get; set; }
		public NotifyValue<User> User { get; set; }
		public NotifyValue<Stat> Stat { get; set; }
		public NotifyValue<bool> IsDataLoaded { get; set; }
		public NotifyValue<int> NewMailsCount { get; set; }

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

		protected override void OnActivate()
		{
			base.OnActivate();
			Navigator.Activate();
		}

		public override IEnumerable<IResult> OnViewReady()
		{
			Bus.SendMessage("Startup");
			if (Config.Cmd.Match("import")) {
				return Import();
			}
			if (Config.Cmd.Match("start-check")) {
				TryClose();
			}
			else {
				return StartCheck();
			}
			return Enumerable.Empty<IResult>();
		}

		public override void CanClose(Action<bool> callback)
		{
			if (Config.Quiet) {
				base.CanClose(callback);
				return;
			}

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

			if (Stat.Value.OrdersCount > 0
				&& Confirm("Обнаружены не отправленные заказы. Отправить их сейчас?")) {
				Coroutine.BeginExecute(SendOrders().GetEnumerator(), callback: (s, a) => base.CanClose(callback));
			}
			else {
				base.CanClose(callback);
			}
		}

		public void UpdateStat()
		{
			Stat.Value = Models.Stat.Update(session, CurrentAddress);
		}

		public IEnumerable<IResult> StartCheck()
		{
			if (!Settings.Value.IsValid)
				CheckSettings();

			if (!Settings.Value.IsValid)
				return Enumerable.Empty<IResult>();

			if (!Config.Quiet) {
				var request = Settings.Value.CheckUpdateCondition();
				if (!String.IsNullOrEmpty(request) && Confirm(request))
					return Update();
			}

			if (User.Value != null
				&& User.Value.IsDeplayOfPaymentEnabled
				&& Settings.Value.LastLeaderCalculation != DateTime.Today) {
				RunTask(new WaitViewModel("Пересчет отсрочки платежа"),
					t => {

						try {
							DbMaintain.UpdateLeaders(statelessSession, Settings.Value);
						}
						catch (Exception e) {
							Console.WriteLine(e);
						}
						return Enumerable.Empty<IResult>();
					}, r => {  });
			}
			return Enumerable.Empty<IResult>();
		}

		private bool CheckSettings()
		{
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

			NewMailsCount.Value = session.Query<Mail>().Count(m => m.IsNew);
			Settings.Value = session.Query<Settings>().First();
			User.Value = session.Query<User>().FirstOrDefault();
			Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.Where(a => a.Id == addressId)
				.DefaultIfEmpty(Addresses.FirstOrDefault())
				.FirstOrDefault();
			defaultItem.Update();
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
			windowManager.ShowDialog(
				new PrintPreviewViewModel(printResult),
				null,
				new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				});
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
			NavigateRoot(new CatalogViewModel());
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
			NavigateRoot(model);
		}

		public bool CanShowMnn
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowMnn()
		{
			NavigateRoot(new MnnViewModel());
		}

		public bool CanSearchOffers
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void SearchOffers()
		{
			NavigateRoot(new SearchOfferViewModel());
		}

		public void ShowSettings()
		{
			windowManager.ShowFixedDialog(new SettingsViewModel());
			//настройки будут обновлены автоматически но в случае если
			//мы показали форму принудительно что бы человек заполнил имя пользователя и пароль
			//это будет слишком поздно
			session.Refresh(Settings.Value);
		}

		public bool CanShowOrderLines
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowOrderLines()
		{
			NavigateRoot(new OrderLinesViewModel());
		}

		public bool CanShowJunkOffers
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowJunkOffers()
		{
			NavigateRoot(new JunkOfferViewModel());
		}

		public bool CanShowRejects
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowRejects()
		{
			NavigateRoot(new RejectsViewModel());
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
				NavigateRoot(new OrdersViewModel());
		}

		public bool CanShowWaybills
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public void ShowRejectedWaybills()
		{
			var rejectStat = statelessSession.Query<WaybillLine>().Where(l => l.IsRejectNew || l.IsRejectCanceled)
				.GroupBy(l => l.Waybill.Address.Id)
				.Select(g => new { addressId = g.Key, count = g.Count()})
				.ToArray();
			var addressId = rejectStat.OrderByDescending(s => s.count).Select(s => s.addressId).FirstOrDefault();
			CurrentAddress = addresses.First(a => a.Id == addressId);

			var model = new WaybillsViewModel();
			model.RejectFilter.Value = RejectFilter.Changed;
			model.Begin.Value = DateTime.Today.AddDays(-Settings.Value.TrackRejectChangedDays);
			NavigateRoot(model);
		}

		public void ShowWaybills()
		{
			NavigateRoot(new WaybillsViewModel());
		}

		public void ShowMain()
		{
			NavigateRoot(defaultItem);
		}

		public void ShowMails()
		{
			NavigateRoot(new Mails());
		}

		public void ShowJournal()
		{
			NavigateRoot(new Journal());
		}

		public bool CanMicroUpdate
		{
			get { return Settings.Value.LastUpdate != null; }
		}

		public IEnumerable<IResult> MicroUpdate()
		{
			return Sync(new UpdateCommand {
				SyncData = "Waybills"
			});
		}

		public IEnumerable<IResult> Update()
		{
			return Sync(new UpdateCommand());
		}

		public bool CanSendOrders
		{
			get
			{
				return Stat.Value.ReadyForSendOrdersCount > 0 && CurrentAddress != null;
			}
		}

		public IEnumerable<IResult> SendOrders(bool force = false)
		{
			if (!CanSendOrders)
				yield break;

			if (Settings.Value.ConfirmSendOrders && !Confirm("Вы действительно хотите отправить заказы?"))
				yield break;

			var warningOrders = statelessSession.Query<Order>()
				.Fetch(o => o.Price)
				.Fetch(o => o.MinOrderSum)
				.ReadyToSend(CurrentAddress)
				.Where(o => o.Sum < o.MinOrderSum.MinOrderSum).ToList();
			if (warningOrders.Count > 0) {
				var orderWarning = new OrderWarning(warningOrders);
				yield return new DialogResult(orderWarning, sizeToContent: true);
			}

			var results = Sync(new SendOrders(CurrentAddress, force));
			foreach (var result in results)
				yield return result;
		}

		private IEnumerable<IResult> Import()
		{
			var command = new UpdateCommand();
			return Sync(command,
				c => c.Process(() => {
					((UpdateCommand)c).Import();
					return UpdateResult.OK;
				}));
		}

		public void CheckDb()
		{
			RunTask(
				new WaitViewModel("Производится восстановление базы данных.\r\nПодождите..."),
				token => {
					var command = new RepairDb();
					command.Token = token;
					command.Config = Config;
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
					command.Config = Config;
					command.Execute();
					return command.Result;
				},
				t => Update());
			Reload();
		}

		protected bool Confirm(string text)
		{
			return windowManager.Question(text) == MessageBoxResult.Yes;
		}

		private IEnumerable<IResult> Sync(RemoteCommand command)
		{
			return Sync(command, c => c.Run());
		}

		private IEnumerable<IResult> Sync(RemoteCommand command, Func<RemoteCommand, UpdateResult> func)
		{
			if (!CheckSettings())
				return Enumerable.Empty<IResult>();

			if(UnitTesting)
				command = OnCommandExecuting(command);

			var progress = new BehaviorSubject<Progress>(new Progress());
			var wait = new SyncViewModel(progress) {
				GenericErrorMessage = command.ErrorMessage
			};
			command.Config = Config;
			command.Progress = progress;

			var results = new IResult[0];
			RunTask(wait,
				t => {
					//настраивать комманду нужно каждый раз тк учетны данные могут быть изменены в RunTask
					command.Configure(Settings.Value);
					command.Token = t;
					return func(command);
				},
				t => {
					if (t.Result == UpdateResult.UpdatePending) {
						RunUpdate();
					}
					else if (t.Result == UpdateResult.OK) {
						Reload();
						windowManager.Notify(command.SuccessMessage);
					}
					results = command.Results.ToArray();
				});
			return results;
		}

		private void RunUpdate()
		{
			windowManager.Warning("Получена новая версия программы. Сейчас будет выполнено обновление.");
			var updateExePath = Path.Combine(Config.UpdateTmpDir, "update", "Updater.exe");
			StartProcess(updateExePath, String.Format("{0} \"{1}\"",
				Process.GetCurrentProcess().Id,
				GetType().Assembly.Location));
			//не нужно ничего запрашивать нужно просто выйти
			Config.Quiet = true;
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

				//если это вторая итерация то нужно пересоздать cancellation
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
						if (!done) {
							session.Refresh(Settings.Value);
						}
					}
				}
			} while (!done);
		}

		public IEnumerable<IScreen> NavigationStack
		{
			get { return Navigator.NavigationStack; }
		}

		private void NavigateRoot(IScreen screen)
		{
			Navigator.NavigateRoot(screen);
		}

		public void Navigate(IScreen item)
		{
			Navigator.Navigate(item);
		}

		public void ResetNavigation()
		{
			Navigator.ResetNavigation();
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			Navigator.NavigateAndReset(views);
		}

		protected override void OnActivationProcessed(IScreen item, bool success)
		{
			var screen = item as BaseScreen;
			if (screen != null) {
				screen.PostActivated();
			}
		}

#if DEBUG
		public Debug Debug { get; set; }

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

		public void ShowDebug()
		{
			windowManager.ShowWindow(Debug);
		}
#endif
		public void Dispose()
		{
			IsNotifying = false;

			foreach (var screen in Navigator.NavigationStack.OfType<IDisposable>())
				screen.Dispose();
			((Stack<IScreen>)Navigator.NavigationStack).Clear();

			if (ActiveItem is IDisposable) {
				var item = ((IDisposable)ActiveItem);
				ActiveItem = null;
				item.Dispose();
			}

			if (session != null) {
				session.Dispose();
				session = null;
			}

			if (statelessSession != null) {
				statelessSession.Dispose();
				statelessSession = null;
			}

			if (defaultItem != null) {
				defaultItem.Dispose();
				defaultItem = null;
			}
			CloseDisposable.Dispose();
		}
	}
}