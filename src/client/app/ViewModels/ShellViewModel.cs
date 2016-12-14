using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Diadok;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using ReactiveUI;
using Xceed.Wpf.Toolkit;
using Address = AnalitF.Net.Client.Models.Address;
using LogManager = log4net.LogManager;
using ILog = log4net.ILog;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;
using WindowState = System.Windows.WindowState;

namespace AnalitF.Net.Client.ViewModels
{
	public interface IPrintable
	{
		bool CanPrint { get; }

		PrintResult Print();
	}

	public interface IPrintableStock
	{
		bool CanPrintStock { get; }
		bool IsView { get; set; }
		ObservableCollection<MenuItem> PrintStockMenuItems { get; set; }
		PrintResult PrintStock();
		string LastOperation { get; set; }
		string PrinterName { get; set; }
		void SetMenuItems();
	}

#if DEBUG
	public class RestoreData
	{
		public RestoreData()
		{
		}

		public RestoreData(object screen)
		{
			if (screen is BaseScreen)
				Args = ((BaseScreen)screen).GetRebuildArgs();
			else
				Args = new object[0];
			TypeName = screen.GetType().FullName;
		}

		public string TypeName;
		public object[] Args;

		public override string ToString()
		{
			return string.Format("{0} - {1}", TypeName, Args.Implode(a => String.Format("{1}:{0}", a, a.GetType())));
		}
	}
#endif

	[DataContract]
	public class ShellViewModel : BaseShell, IDisposable
	{
		private ManualResetEventSlim startSync = new ManualResetEventSlim();
		private WindowManager windowManager;
		private ISession session;
		private ILog log = LogManager.GetLogger(typeof(ShellViewModel));
		private List<Address> addresses = new List<Address>();

		[DataMember]
		public Dictionary<string, List<ColumnSettings>> ViewSettings = new Dictionary<string, List<ColumnSettings>>();
		[DataMember]
		public Dictionary<string, string> ViewModelSettings = new Dictionary<string, string>();
		public Dictionary<string, object> SessionContext = new Dictionary<string, object>();
		[DataMember]
		public Dictionary<string, object> PersistentContext = new Dictionary<string, object>();

		public Subject<string> Notifications = new Subject<string>();
		public NotifyValue<List<Schedule>> Schedules = new NotifyValue<List<Schedule>>(new List<Schedule>());
		//параметры авто-комментария должны быть одинаковыми на время работы приложения
		public bool ResetAutoComment;
		public string AutoCommentText;

		private bool _leaderCalculationWasStart;
		public  bool LeaderCalculationWasStart
		{
			get { return _leaderCalculationWasStart; }
			set
			{
				_leaderCalculationWasStart = value;
				NotifyOfPropertyChange(nameof(LeaderCalculationWasStart));
			}
		}

		//не верь решарперу
		public ShellViewModel()
			: this(new Config.Config())
		{
		}

		public ShellViewModel(Config.Config config)
		{
			DisplayName = "АналитФАРМАЦИЯ";
			BaseScreen.InitFields(this);
			SpecialMarkupProducts.Value = new uint[0];
			Stat.Value = new Stat();
			Config = config;
			CloseDisposable.Add(CancelDisposable);
			PendingDownloads = new ObservableCollection<Loadable>();
			IsView = true;

#if DEBUG
			if (!Env.IsUnitTesting)
				Debug = new Debug();
#endif

			Settings
				.Select(v => v?.LastUpdate != null)
				.Subscribe(IsDataLoaded);
			Version = typeof(ShellViewModel).Assembly.GetName().Version.ToString();

			session = Env.Factory?.OpenSession();
			windowManager = (WindowManager)IoC.Get<IWindowManager>();

			this.ObservableForProperty(m => m.ActiveItem)
				.Merge(User.Cast<object>())
				.Subscribe(_ => UpdateDisplayName());

			Stat.CombineLatest(CurrentAddress, (x, y) => x?.ReadyForSendOrdersCount > 0 && y != null)
				.Subscribe(CanSendOrders);

			CurrentAddress.Subscribe(_ => {
				UpdateStat();
				//текущий адрес может измениться как при выборе пользователем
				//так и после обновления когда мы перезагрузим данные
				//если ActiveItem?.IsActive == false значит перезагрузка происходит из-за обновления данных
				//в этом случае не нужно обновлять форму это выполнит код обработки обновления
				if (ActiveItem?.IsActive == true) {
					ActiveItem?.Deactivate(false);
					Env.Bus.SendMessage("Changed", "db");
					ActiveItem?.Activate();
				}
			});

			Settings
				.Subscribe(_ => {
					NotifyOfPropertyChange(nameof(CanShowCatalog));
					NotifyOfPropertyChange(nameof(CanSearchOffers));
					NotifyOfPropertyChange(nameof(CanShowMnn));
					NotifyOfPropertyChange(nameof(CanShowPrice));
					NotifyOfPropertyChange(nameof(CanShowMinCosts));

					NotifyOfPropertyChange(nameof(CanShowOrders));
					NotifyOfPropertyChange(nameof(CanShowOrderLines));

					NotifyOfPropertyChange(nameof(CanShowJunkOffers));
					NotifyOfPropertyChange(nameof(CanShowRejects));
					NotifyOfPropertyChange(nameof(CanShowWaybills));
					NotifyOfPropertyChange(nameof(CanMicroUpdate));
					NotifyOfPropertyChange(nameof(CanShowBatch));
					NotifyOfPropertyChange(nameof(CanShowAwaited));
					NotifyOfPropertyChange(nameof(CanLoadWaybillHistory));
					NotifyOfPropertyChange(nameof(CanLoadOrderHistory));

					NotifyOfPropertyChange(nameof(CanShowInventory));
					NotifyOfPropertyChange(nameof(CanShowFrontend));
				});

			CloseDisposable.Add(Env.Bus.Listen<Loadable>().ObserveOn(Env.UiScheduler).Subscribe(l => {
				CloseDisposable.Add(l.RequstCancellation);
				PendingDownloads.Add(l);
			}));
			CloseDisposable.Add(Env.Bus.Listen<Loadable>("completed").ObserveOn(Env.UiScheduler).Subscribe(l => {
				CloseDisposable.Remove(l.RequstCancellation);
				PendingDownloads.Remove(l);
			}));
			CloseDisposable.Add(Env.Bus.Listen<Stat>().Subscribe(e => Stat.Value = new Stat(e, Stat.Value)));

			Schedules.Select(_ =>
				Schedules.Value.Count == 0
					? Observable.Empty<bool>()
					: Observable.Timer(TimeSpan.Zero, 20.Second(), Env.UiScheduler)
						.Select(__ => Schedule.IsOutdate(Schedules.Value, Settings.Value.LastUpdate.GetValueOrDefault())))
				.Switch()
				.Where(k => k)
				.SelectMany(_ => RxHelper.ToObservable(UpdateBySchedule()))
				.Subscribe(r => ResultsSink.OnNext(r));

			CanExport = this.ObservableForProperty(m => m.ActiveItem)
				.Select(e => e.Value is IExportable
					? ((IExportable)e.Value).CanExport
					: Observable.Return(false))
				.Switch()
				.ToValue(CancelDisposable);
			CanPrint = this.ObservableForProperty(m => m.ActiveItem)
				.Select(e => e.Value is IPrintable
					? ((IPrintable)e.Value).ObservableForProperty(m => m.CanPrint, skipInitial: false)
					: Observable.Return(new ObservedChange<IPrintable, bool>()))
				.Switch()
				.Select(e => e.Value)
				.ToValue(CancelDisposable);
				CanPrintPreview = CanPrint.ToValue();

			CanPrintStock = this.ObservableForProperty(m => m.ActiveItem)
				.Select(e => e.Value is IPrintableStock
					? ((IPrintableStock)e.Value).ObservableForProperty(m => m.CanPrintStock, skipInitial: false)
					: Observable.Return(new ObservedChange<IPrintableStock, bool>()))
				.Switch()
				.Select(e => e.Value)
				.ToValue(CancelDisposable);

			PrintStockMenuItems = this.ObservableForProperty(m => m.ActiveItem)
				.Select(e => e.Value is IPrintableStock
					? ((IPrintableStock)e.Value).ObservableForProperty(m => m.PrintStockMenuItems, skipInitial: false, beforeChange: true)
					: Observable.Return(new ObservedChange<IPrintableStock, ObservableCollection<MenuItem>>()))
				.Switch()
				.Select(e => e.Value)
				.ToValue(CancelDisposable);

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => SetMenuItems());

			//if (Env.Factory != null) {
			//	var task = TaskEx.Run(() => Models.Inventory.SyncCommand.Start(config, startSync, CancelDisposable.Token).Wait());
			//	CloseDisposable.Add(Disposable.Create(() => {
			//		CancelDisposable.Dispose();
			//		task.Wait(TimeSpan.FromSeconds(10));
			//	}));
			//}
		}

		public Config.Config Config { get; set; }

		public ObservableCollection<Loadable> PendingDownloads { get; set; }

		public NotifyValue<Settings> Settings { get; set; }
		public NotifyValue<uint[]> SpecialMarkupProducts { get; set; }
		public NotifyValue<User> User { get; set; }
		public NotifyValue<Stat> Stat { get; set; }
		public NotifyValue<bool> IsDataLoaded { get; set; }
		public NotifyValue<int> NewMailsCount { get; set; }
		public NotifyValue<int> NewDocsCount { get; set; }
		public NotifyValue<string[]> Instances { get; set; }

		public string Version { get; set; }

		public List<Address> Addresses
		{
			get { return addresses; }
			set
			{
				addresses = value;
				NotifyOfPropertyChange(nameof(Addresses));
			}
		}

		[DataMember]
		public NotifyValue<Address> CurrentAddress { get; set; }

		protected override void OnInitialize()
		{
			Env.Query(x => x.CreateSQLQuery("update Orders set Send = 1 where Send = 0 and Frozen = 0").ExecuteUpdate()).LogResult();
			Instances.Value = new string[0];
			if (Directory.Exists(Config.Opt)) {
				Instances.Value = Directory.GetDirectories(Config.Opt).Select(x => Path.GetFileName(x)).ToArray();
			}
			Reload();
			if (Settings.Value.TabbedUI)
				Navigator = new TabNavigator(this);
			else
				Navigator = new Navigator(this);
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			ShowMain();
		}

		public override IEnumerable<IResult> OnViewReady()
		{
			Env.Bus.SendMessage("Startup");
			if (Config.Cmd.Match("import")) {
				//если в папке с обновлением есть данные то мы должны их импортировать
				//что бы не потерять накладные
				if (Directory.Exists(Config.UpdateTmpDir) && Directory.GetFiles(Config.UpdateTmpDir, "*.meta.txt").Length > 0) {
					using (var command = new UpdateCommand())
						return Sync(command, c => c.Process(() => {
							((UpdateCommand)c).Import();
							return UpdateResult.OK;
					}));
				}
				//флаг import говорит что мы обновились на новую версию
				//но обновление может быть не выполнено
				//если мы просто запросим обновление то мы будем ходить в бесконечном цикле
				//что бы избежать этого говорим серверу что проверять обновление версии не следует
				return Sync(new UpdateCommand {
					SyncData = "NoBin"
				});
			}
			if (Config.Cmd.Match("migrate"))
				return Migrate();
			if (Config.Cmd.Match("start-check")) {
				TryClose();
				return Enumerable.Empty<IResult>();
			}
			else {
				var results = StartCheck();
				if ((Config.Cmd ?? "").StartsWith("batch=")) {
					//вызов Batch должен происходить только после операций в startcheck
					//тк startcheck может изменить данные а batch эти изменения не увидит
					results = results.Concat(LazyHelper.Create(() => Batch(Config.Cmd.Remove(0, 6))));
				}
				return results;
			}
		}

		public void CloseActive()
		{
			((TabNavigator)Navigator).CloseScreen(ActiveItem);
		}

		public void CloseScreen(IScreen item)
		{
			((TabNavigator)Navigator).CloseScreen(item);
		}

		public async Task CheckCloseConditions()
		{
				var orderDays = -Settings.Value.DeleteOrdersOlderThan;
				var orders = await Env.Query(s => s.Query<SentOrder>()
					.Where(w => w.SentOn < DateTime.Today.AddDays(orderDays)).Take(1000).ToArray());
				if (orders.Any()) {
					var deleteOldOrders = !Settings.Value.ConfirmDeleteOldOrders ||
						Confirm("В архиве заказов обнаружены заказы," +
							$" сделанные более {Settings.Value.DeleteOrdersOlderThan} дней назад. Удалить их?");
					if (deleteOldOrders)
						await Env.Query(s => s.DeleteEach(orders));
				}

				var waybillDays = -Settings.Value.DeleteWaybillsOlderThan;
				var waybills = await Env.Query(s => s.Query<Waybill>()
					.Where(w => w.WriteTime < DateTime.Today.AddDays(waybillDays)).Take(1000).ToArray());
				if (waybills.Any()) {
					var deleteOldWaybills = !Settings.Value.ConfirmDeleteOldWaybills ||
						Confirm("В архиве заказов обнаружены документы (накладные, отказы)," +
							$" сделанные более {Settings.Value.DeleteWaybillsOlderThan} дней назад. Удалить их?");
					if (deleteOldWaybills)
						await Env.Query(s => s.DeleteEach(waybills));
				}
		}

		public override void CanClose(Action<bool> callback)
		{
			if (Config.Quiet && !Settings.Value.DebugLoud) {
				base.CanClose(callback);
				return;
			}

			CheckCloseConditions().ContinueWith(t => {
				if (t.IsFaulted)
					log.Error("Ошибка при проверки условий закрытия", t.Exception);
				if (Stat.Value.ReadyForSendOrdersCount > 0
					&& Confirm("Обнаружены не отправленные заказы. Отправить их сейчас?")) {
					Coroutine.BeginExecute(SendOrders().GetEnumerator(), callback: (s, a) => base.CanClose(callback));
				}
				else {
					base.CanClose(callback);
				}
			}, Env.TplUiScheduler);
		}

		public void UpdateStat()
		{
			Env.RxQuery(x => Models.Stat.Update(x, CurrentAddress.Value)).Subscribe(Stat);
		}

		public IEnumerable<IResult> StartCheck()
		{
			if (!Settings.Value.IsValid)
				CheckSettings();

			if (!Settings.Value.IsValid)
				return Enumerable.Empty<IResult>();

			if (!Config.Quiet) {
				if (Schedule.IsOutdate(Schedules.Value, Settings.Value.LastUpdate.GetValueOrDefault())) {
					return UpdateBySchedule();
				}
				var request = Settings.Value.CheckUpdateCondition();
				if (!String.IsNullOrEmpty(request) && Confirm(request))
					return Update();
			}

			if (User.Value?.IsDelayOfPaymentEnabled == true
				&& Settings.Value.LastLeaderCalculation != DateTime.Today) {
				var result = new Models.Results.TaskResult(UpdateLeaders(), new WaitViewModel("Пересчет отсрочки платежа"));
				IoC.BuildUp(result);
				result.Execute(new ActionExecutionContext());
			}

#if DEBUG
			if (!Env.IsUnitTesting) {
				var disposable = new CompositeDisposable();
				try {
					var items = PersistentNavigationStack
						.Where(t => t.TypeName != typeof(Main).FullName)
						.Select(t => {
							var type = Type.GetType(t.TypeName);
							if (type == null)
								return null;
							var constructor = type.GetConstructor(t.Args.Select(x => x.GetType()).ToArray());
							if (constructor == null)
								return null;
							var v = Activator.CreateInstance(type, t.Args);
							if (v is IDisposable) {
								disposable.Add((IDisposable)v);
							}
							return v;
						})
						.Where(x => x != null)
						.OfType<IScreen>()
						.ToArray();
					if (Navigator is Navigator) {
						((Navigator)Navigator).NavigateAndReset(items);
					} else {
						foreach (var item in items) {
							Navigator.Navigate(item);
						}
					}
				}
				catch(Exception e) {
					disposable.Dispose();
					log.Error("Не удалось восстановить состояние", e);
				}
			}
#endif
			return Enumerable.Empty<IResult>();
		}

		private bool CheckSettings()
		{
			if (!Settings.Value.IsValid) {
				windowManager.Warning("Для начала работы с программой необходимо заполнить учетные данные");
				ShowSettings("LoginTab");
				return false;
			}
			return true;
		}

		public void Reload()
		{
			if (session == null)
				return;

			session.Clear();

			//нужно сохранить идентификатор выбранного адреса доставки тк
			//строка Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			//сбросит его
			var addressId = CurrentAddress.Value?.Id;
			Settings.Value = session.Query<Settings>().First();
			User.Value = session.Query<User>().FirstOrDefault();
			Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			var addressConfigs = session.Query<AddressConfig>().ToArray();
			Addresses.Each(x => x.Config = addressConfigs.FirstOrDefault(y => y.Address == x));
			CurrentAddress.Value = Addresses.Where(a => a.Id == addressId)
				.DefaultIfEmpty(Addresses.FirstOrDefault())
				.FirstOrDefault();

			Env.RxQuery(x => x.Query<Schedule>().ToList())
				.Subscribe(Schedules);
			Env.RxQuery(x => x.Query<Mail>().Count(m => m.IsNew))
				.Subscribe(NewMailsCount);
			Env.RxQuery(x => x.Query<Waybill>().Count(m => m.IsNew))
				.Subscribe(NewDocsCount);
			Env.RxQuery(x => SpecialMarkupCatalog.Load(x.Connection))
				.Subscribe(SpecialMarkupProducts);

			//изменились заявки
			Env.Bus.SendMessage("Changed", "db");
			//изменилась база данных
			Env.Bus.SendMessage("Reload", "db");
			ActiveItem?.Activate();
		}

		protected void UpdateDisplayName()
		{
			var value = "АналитФАРМАЦИЯ";

			if (User.Value != null && !String.IsNullOrEmpty(User.Value.Name)) {
				value += " - " + User.Value.Name;
			}

			var named =  ActiveItem as IHaveDisplayName;
			if (named != null && !String.IsNullOrEmpty(named.DisplayName)) {
				value += " - " + named.DisplayName;
			}
			DisplayName = value;
		}

		public NotifyValue<bool> CanExport { get; set; }

		public IResult Export()
		{
			if (!CanExport)
				return null;

			return ((IExportable)ActiveItem).Export();
		}

		public NotifyValue<bool> CanPrint { get; set; }

		public IResult Print()
		{
			if (!CanPrint.Value)
				return null;

			return ((IPrintable)ActiveItem).Print();
		}

		public NotifyValue<bool> CanPrintStock { get; set; }
		public NotifyValue<ObservableCollection<MenuItem>> PrintStockMenuItems { get; set;}
		public string PrinterName { get; set; }
		public bool IsView { get; set; }
		public static RoutedCommand PrintStockRoutedCommand = new RoutedCommand();

		public IResult PrintStock()
		{
			if (!CanPrintStock.Value)
				return null;
			return ((IPrintableStock) ActiveItem).PrintStock();
		}

		private void ExecutedPrintStockCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var item = (MenuItem) sender;
			var printableStock = (IPrintableStock) ActiveItem;
			printableStock.IsView = IsView;
			printableStock.LastOperation = item.Header.ToString();
			if (IsView)
				printableStock.PrintStock();
		}

		private void CanExecutePrintStocCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CanPrintStock;
		}

		public void SetMenuItems()
		{
			if (!CanPrintStock)
				return;
			PrintStockMenuItems.Value.Clear();
			((IPrintableStock) ActiveItem).SetMenuItems();
			CommandBinding commandBinding = new CommandBinding(PrintStockRoutedCommand, ExecutedPrintStockCommand,
				CanExecutePrintStocCommand);
			foreach (var it in PrintStockMenuItems.Value) {
				it.CommandBindings.Add(commandBinding);
				it.Command = PrintStockRoutedCommand;
			}
			var item = new MenuItem {Header = "Настройки"};
			item.Click += (sender, args) => Coroutine.BeginExecute(ReportSetting().GetEnumerator());
			PrintStockMenuItems.Value.Add(item);
		}

		public IEnumerable<IResult> ReportSetting()
		{
			var req = new ReportSetting();
			yield return new DialogResult(req);
			PrinterName = req.PrinterName;
			if (req.IsView) {
				IsView = true;
				PrintStockMenuItems.Value.Where(m=>m.HeaderStringFormat != "Настройки").Each(m=>m.IsCheckable = false);
				PrintStockMenuItems.Value.Where(m=>m.HeaderStringFormat != "Настройки").Each(m=>m.IsChecked = false);
			}

			if (req.IsPrint) {
				IsView = false;
				PrintStockMenuItems.Value.Where(m=>m.HeaderStringFormat != "Настройки").Each(m=>m.IsCheckable = true);
			}

		}


		public NotifyValue<bool> CanPrintPreview { get; set; }

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

		//отдельные свойства нужны для того что бы к ним могли привязаться пункты меню
		public bool CanShowCatalog => Settings.Value.LastUpdate != null;

		public void ShowCatalog()
		{
			var catalog = ActiveItem as CatalogViewModel;
			if (catalog != null && catalog.CatalogSearch.Value) {
				catalog.CatalogSearch.Value = false;
			}
			NavigateRoot(new CatalogViewModel());
		}

		public bool CanShowPrice => Settings.Value.LastUpdate != null;

		public void ShowPrice()
		{
			var model = new PriceViewModel {
				OpenSinglePrice = true
			};
			NavigateRoot(model);
		}

		public bool CanShowMinCosts => Settings.Value.LastUpdate != null;

		public void ShowMinCosts()
		{
			if (LeaderCalculationWasStart)
			{
				MessageResult.Warn("Идет расчет минимальных цен. Минимальные цены можно будет посмотреть после окончания расчета, это может занять какое-то время. Пожалуйста, подождите и повторно откройте \"Минимальные цены\"")
					.Execute(new ActionExecutionContext());
				return;
			}

			NavigateRoot(new Offers.MinCosts());
		}

		public bool CanShowAwaited => Settings.Value.LastUpdate != null;

		public void ShowAwaited()
		{
			NavigateRoot(new Awaited());
		}

		public bool CanShowMnn => Settings.Value.LastUpdate != null;

		public void ShowMnn()
		{
			NavigateRoot(new MnnViewModel());
		}

		public bool CanSearchOffers => Settings.Value.LastUpdate != null;

		public void SearchOffers()
		{
			NavigateRoot(new SearchOfferViewModel());
		}

		//для вызова из меню
		public bool ShowSettings()
		{
			return ShowSettings(tab: null);
		}

		public bool ShowSettings(string tab)
		{
			var model = new SettingsViewModel();
			if (tab != null)
				model.SelectedTab.Value = tab;
			windowManager.ShowFixedDialog(model);
			//настройки будут обновлены автоматически но в случае если
			//мы показали форму принудительно что бы человек заполнил имя пользователя и пароль
			//это будет слишком поздно
			if (session != null) {
				session.Evict(Settings.Value);
				Settings.Value = session.Query<Settings>().First();
				Env.RxQuery(x => SpecialMarkupCatalog.Load(x.Connection))
					.Subscribe(SpecialMarkupProducts);
			}
			return model.IsCredentialsChanged;
		}

		public bool CanShowInventory => Settings.Value.LastUpdate != null;

		public void ShowInventory()
		{
			NavigateRoot(new Inventory.Stocks());
		}

		public bool CanShowFrontend => Settings.Value.LastUpdate != null;

		public void ShowFrontend()
		{
			NavigateRoot(new Inventory.Frontend());
		}

		public bool CanShowOrderLines => Settings.Value.LastUpdate != null;

		public void ShowOrderLines()
		{
			NavigateRoot(new OrderLinesViewModel());
		}

		public bool CanShowJunkOffers => Settings.Value.LastUpdate != null;

		public void ShowJunkOffers()
		{
			NavigateRoot(new JunkOfferViewModel());
		}

		public bool CanShowRejects => Settings.Value.LastUpdate != null;

		public void ShowRejects()
		{
			NavigateRoot(new RejectsViewModel());
		}

		public bool CanShowOrders => Settings.Value.LastUpdate != null;

		public void ShowOrders()
		{
			NavigateRoot(new OrdersViewModel());
		}

		public bool CanShowBatch => Settings.Value.LastUpdate != null;

		public void ShowBatch()
		{
			if (!CanShowBatch)
				return;
			NavigateRoot(new Batch());
		}

		public bool CanShowWaybills => Settings.Value.LastUpdate != null;

		public void ShowRejectedWaybills()
		{
			var rejectStat = Env.Query(s => {
				return s.Query<WaybillLine>().Where(l => l.IsRejectNew || l.IsRejectCanceled)
					.GroupBy(l => l.Waybill.Address.Id)
					.Select(g => new { addressId = g.Key, count = g.Count()})
					.ToArray();
			}).Result;
			//мы можем найти отказ в накладной которая принадлежит адресу который больше не доступен клиенту
			//по этому выбранные идентификаторы нужно сопоставить с существующими адресами
			var address = rejectStat.OrderByDescending(s => s.count).Select(s => s.addressId)
				.Select(x => addresses.FirstOrDefault(y => y.Id == x))
				.FirstOrDefault(x => x != null);
			CurrentAddress.Value = address ?? CurrentAddress.Value;

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
			NavigateRoot(new Main());
		}

		public void ShowMails()
		{
			NavigateRoot(new Mails());
		}

		public void ShowExtDocs()
		{
			NavigateRoot(new Index());
		}

		public void ShowSbis()
		{
			NavigateRoot(new Sbis.Index());
		}

		public void ShowJournal()
		{
			NavigateRoot(new Journal());
		}

		public void CheckDefectSeries()
		{
			NavigateRoot(new CheckDefectSeries());
		}

		public void ReceivingOrders()
		{
			NavigateRoot(new WaybillsViewModel());
		}

		public void Checks()
		{
			NavigateRoot(new Checks());
		}

		public void InventoryDocs()
		{
			NavigateRoot(new InventoryDocs());
		}

		public void WriteoffDocs()
		{
			NavigateRoot(new WriteoffDocs());
		}

		public void ReturnToSuppliers()
		{
			NavigateRoot(new ReturnToSuppliers());
		}

		public void ReassessmentDocs()
		{
			NavigateRoot(new ReassessmentDocs());
		}

		public void DisplacementDocs()
		{
			NavigateRoot(new DisplacementDocs());
		}

		public bool CanMicroUpdate => Settings.Value.LastUpdate != null;

		public IEnumerable<IResult> MicroUpdate()
		{
			return Sync(new UpdateCommand {
				SyncData = "Waybills"
			});
		}

		public IEnumerable<IResult> CleanSync()
		{
			if (!Confirm("Кумулятивное обновление достаточно длительный процесс. Продолжить?"))
				yield break;
			if (User.Value != null)
				User.Value.LastSync = null;
			foreach (var result in Sync(new UpdateCommand())) {
					yield return result;
			}
		}

		public bool CanLoadOrderHistory => Settings.Value.LastUpdate != null;

		public IEnumerable<IResult> LoadOrderHistory()
		{
			return Sync(new UpdateCommand {
				SyncData = "OrderHistory"
			});
		}

		public bool CanLoadWaybillHistory => Settings.Value.LastUpdate != null;

		public IEnumerable<IResult> LoadWaybillHistory()
		{
			return Sync(new UpdateCommand {
				SyncData = "WaybillHistory"
			});
		}

		public IEnumerable<IResult> Update()
		{
			var result = Sync(new UpdateCommand());
			UpdateLeaders().LogResult();
			return result;
		}

		public async Task UpdateLeaders()
		{
			LeaderCalculationWasStart = true;
			await Env.Query(s => DbMaintain.UpdateLeaders(s));
			LeaderCalculationWasStart = false;
		}

		private IEnumerable<IResult> UpdateBySchedule()
		{
			if (!Schedule.CanStartUpdate()) {
				log.WarnFormat("Обновление по расписанию не может быть запущено тк есть открытые диалоговые окна");
				yield break;
			}
			yield return new DialogResult(new SelfClose("Сейчас будет произведено обновление данных\r\nпо установленному расписанию.",
				"Обновление", 10) {
					Scheduler = Env.UiScheduler
				});
			foreach (var result in Update()) {
				yield return result;
			}
		}

		public IEnumerable<IResult> Batch(string fileName = null, BatchMode mode = BatchMode.Normal)
		{
			if (CurrentAddress.Value == null)
				yield break;
			var results = Sync(new UpdateCommand {
				SyncData = "Batch",
				BatchFile = fileName,
				AddressId = CurrentAddress.Value.Id,
				BatchMode = mode
			});
			foreach (var result in results) {
				yield return result;
			}
			ShowBatch();
		}

		public NotifyValue<bool> CanSendOrders { get; set; }

		//для "горячей" клавиши
		public IEnumerable<IResult> SendOrders()
		{
			return SendOrders(false);
		}

		public IEnumerable<IResult> SendOrders(bool force)
		{
			if (!CanSendOrders)
				yield break;

			if (Settings.Value.ConfirmSendOrders && !Confirm("Вы действительно хотите отправить заказы?"))
				yield break;

			//сохраняем изменения на активной форме
			ActiveItem?.Deactivate(false);
			var warningOrders = Env.Query(s => {
				return s.Query<Order>()
					.Fetch(o => o.Price)
					.Fetch(o => o.MinOrderSum)
					.ReadyToSend(CurrentAddress)
					.Where(o => o.Sum < o.MinOrderSum.MinOrderSum)
					.ToList();
			}).Result;
			if (warningOrders.Count > 0) {
				var orderWarning = new OrderWarning(warningOrders);
				yield return new DialogResult(orderWarning);
			}

			var results = Sync(new SendOrders(CurrentAddress, force));
			foreach (var result in results)
				yield return result;
			//говорим форме что изменились данные
			Env.Bus.SendMessage("Changed", "db");
			//говорим форме что пора перезагрузить данные
			ActiveItem?.Activate();
		}

		public IEnumerable<IResult> Migrate()
		{
			using (var command = new UpdateCommand())
				return Sync(command,c => c.Process(() => {
						((UpdateCommand)c).Migrate();
						return UpdateResult.OK;
					}), checkSettings: false);
		}

		public void CheckDb()
		{
			RunCmd(new WaitViewModel("Производится восстановление базы данных.\r\nПодождите..."),
				new RepairDb(),
				t => {
					if (t) {
						windowManager.Notify("Проверка базы данных завершена.\r\nОшибок не найдено.");
					} else {
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

			RunCmd(
				new WaitViewModel("Производится очистка базы данных.\r\nПодождите..."),
				new CleanDb(),
				t => Update());
			Reload();
		}

		public IEnumerable<IResult> Feedback()
		{
			using (var feedback = new Feedback()) {
				yield return new DialogResult(feedback);
				foreach (var result in Sync(new SendFeedback(feedback)))
					yield return result;
			}
		}

		public void Support()
		{
			var file = Directory.GetFiles(FileHelper.MakeRooted("."), "ТехПоддержка*.exe").FirstOrDefault();
			if (String.IsNullOrEmpty(file))
				return;
			StartProcess(file);
		}

		public class CloneSettings
		{
			[Display(Name = "Наименование копии")]
			public string Name { get; set; }
		}

		public void OpenClone(string name)
		{
			var dst = Path.Combine(Config.Opt, name);
			var exe = Directory.GetFiles(dst, "AnalitF*.exe").First();
			StartProcess(exe, workDir: dst);
		}

		public IEnumerable<IResult> Clone()
		{
			var settings = new CloneSettings();
			yield return new DialogResult(new SimpleSettings(settings));
			var name = settings.Name;
			if (String.IsNullOrWhiteSpace(name)) {
				windowManager.Error("Наименование должно быть указано");
				yield break;
			}
			var src = new DirectoryInfo(FileHelper.MakeRooted("."));
			var dst = Path.Combine(Config.Opt, name);
			Directory.CreateDirectory(dst);
			var files = src.GetFiles("*.dll").Concat(src.GetFiles("*.exe")).Concat(src.GetFiles("*.config"));
			foreach (var file in files)
				file.CopyTo(Path.Combine(dst, file.Name), true);
			FileHelper.CopyDir(Path.Combine(src.FullName, "share"), Path.Combine(dst, "share"));
			var exe = Directory.GetFiles(dst, "AnalitF*.exe").First();

			var srcLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "АналитФармация.lnk");
			CreateLink(srcLink, name, exe);
			srcLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "АналитФармация", "АналитФармация.lnk");
			CreateLink(srcLink, name, exe);

			StartProcess(exe, workDir: dst);
			Instances.Value = Directory.GetDirectories(Config.Opt).Select(x => Path.GetFileName(x)).ToArray();
		}

		private static void CreateLink(string srcLink, string name, string exe)
		{
			if (File.Exists(srcLink)) {
				var dstLink = Path.Combine(Path.GetDirectoryName(srcLink), $"АналитФармация - {name}.lnk");
				File.Copy(srcLink, dstLink);
				var link = new ShellLink(dstLink) {
					Target = exe,
					IconPath = exe,
					IconIndex = 0,
					WorkingDirectory = Path.GetDirectoryName(exe),
				};
				link.Save();
			}
		}

		protected bool Confirm(string text)
		{
			return windowManager.Question(text) == MessageBoxResult.Yes;
		}

		public void SyncInventory()
		{
			startSync.Set();
		}

		public void RunCmd<T>(WaitViewModel model, DbCommand<T> cmd, Action<T> success = null)
		{
			RunTask(model,
				t => {
					using (cmd) {
						cmd.Token = t.Token;
						cmd.Config = Config;
						cmd.Execute();
						return cmd.Result;
					}
				},
				t => {
					success?.Invoke(t.Result);
				});
		}

		private IEnumerable<IResult> Sync(RemoteCommand command)
		{
			using (command)
				return Sync(command, c => c.Run());
		}

		private IEnumerable<IResult> Sync(RemoteCommand command,
			Func<RemoteCommand, UpdateResult> func,
			bool checkSettings = true)
		{
			//могут измениться настройки адресов, нужно сохранить изменения
			session?.Flush();
			if (checkSettings && !CheckSettings())
				return Enumerable.Empty<IResult>();

#if DEBUG
			if(Env.IsUnitTesting)
				command = OnCommandExecuting(command);
#endif

			var wait = new SyncViewModel(command.Progress, Env.Scheduler) {
				GenericErrorMessage = command.ErrorMessage
			};
			Dispatcher dispatcher = null;
			if (SynchronizationContext.Current != null)
				dispatcher = Dispatcher.CurrentDispatcher;

			if (command is UpdateCommand) {
				((UpdateCommand)command).ErrorSolver = x => {
					var result = MessageBoxResult.None;
					var action = new System.Action(() => {
						result = windowManager.ShowMessageBox(
							$"При обновлении произошла ошибка {x.Message}\r\nПовторить операцию?",
							"АналитФАРМАЦИЯ: Ошибка",
							MessageBoxButton.YesNo,
							MessageBoxImage.Error);
					});
					if (dispatcher != null)
						dispatcher.Invoke(DispatcherPriority.Normal, action);
					else
						action();
					return result == MessageBoxResult.Yes ? ErrorResolution.TryAgain : ErrorResolution.Fail;
				};
			}
			var results = new IResult[0];
			RunTask(wait,
				t => {
					//настраивать команду нужно каждый раз тк учетные данные могут быть изменены в RunTask
					command.Configure(Settings.Value, Config, t.Token);
					return func(command);
				},
				t => {
					var view = ((ShellView)GetView());
					if (view != null) {
						if (view.WindowState == WindowState.Minimized) {
							view.WindowState = WindowState.Maximized;
						}
						WinApi.SetForegroundWindow(new WindowInteropHelper(view).Handle);
					}
					if (t.Result == UpdateResult.UpdatePending) {
						RunUpdate();
					}
					else if (t.Result == UpdateResult.SilentOk) {
						Reload();
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
			var updateExePath = Path.Combine(Config.BinUpdateDir, "Updater.exe");
			StartProcess(updateExePath, String.Format("{0} \"{1}\"",
				Process.GetCurrentProcess().Id,
				GetType().Assembly.Location));
			//не нужно ничего запрашивать нужно просто выйти
			Config.Quiet = true;
			TryClose();
		}

		private void RunTask<T>(WaitViewModel viewModel, Func<CancellationTokenSource, T> func,
			Action<Task<T>> success = null)
		{
			ActiveItem?.Deactivate(false);

			bool done;
			int count = 0;
			do {
				count++;
				done = true;

				//если это вторая итерация то нужно пересоздать cancellation
				//тк у предыдущего уже будет стоять флаг IsCancellationRequested
				//и ничего не запустится
				viewModel.Cancellation = new CancellationTokenSource();
				var token = viewModel.Cancellation.Token;
				var task = new Task<T>(() => func(viewModel.Cancellation), token);
				task.ContinueWith(t => {
					viewModel.IsCompleted = true;
					viewModel.TryClose();
				}, Env.TplUiScheduler);
				task.Start(TaskScheduler.Default);

				windowManager.ShowFixedDialog(viewModel);

				if (task.IsCanceled) {
					//отмена может произойти как по инициативе пользователя так и если данные не загружаются
					//если пользователь отменил ничего делать не надо
					//если отмена произведена таймером нужно показать сообщение об ошибке
					log.Warn($"Отменена задача {viewModel.DisplayName}");
				} else if (task.IsFaulted) {
					log.Debug($"Ошибка при выполнении задачи {viewModel.DisplayName}", task.Exception);
					var baseException = task.Exception.GetBaseException();
					if (viewModel.Cancellation.IsCancellationRequested) {
						log.Warn($"Отменена задача {viewModel.DisplayName} пользователем");
						return;
					}

					var error = ErrorHelper.TranslateException(task.Exception)
						?? viewModel.GenericErrorMessage;

					log.Error(error, task.Exception);
					windowManager.Error(error);

					//показывать форму с настройками нужно только один раз
					if (count == 1
						&& (baseException as RequestException)?.StatusCode == HttpStatusCode.Unauthorized) {
						done = !ShowSettings("LoginTab");
					}
				} else {
					success?.Invoke(task);
				}
			} while (!done);
		}

		public IEnumerable<IScreen> NavigationStack => Navigator.NavigationStack;

		public void NavigateRoot(IScreen screen)
		{
			Navigator.NavigateRoot(screen);
		}

		public void Navigate(IScreen item)
		{
			Navigator.Navigate(item);
		}

		public void ActivateItemAt(int index)
		{
			if (index < Items.Count)
				ActiveItem = Items[index];
		}

		protected override void OnActivationProcessed(IScreen item, bool success)
		{
			var screen = item as BaseScreen;
			screen?.PostActivated();
		}

		protected override void Configure(IScreen newItem)
		{
			Util.SetValue(newItem, "Manager", windowManager);
			Util.SetValue(newItem, "Shell", this);
			Util.SetValue(newItem, "Env", Env);
		}

#if DEBUG
		public Debug Debug { get; set; }

		[DataMember]
		public List<RestoreData> PersistentNavigationStack = new List<RestoreData>();

		protected override void OnDeactivate(bool close)
		{
			PersistentNavigationStack = NavigationStack.Skip(1)
				.Concat(new [] { ActiveItem })
				.Select(s => new RestoreData(s))
				.ToList();
			base.OnDeactivate(close);
		}

		public void Collect()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		public void Snoop()
		{
			var assembly = Assembly.LoadFrom(@"C:\ProgramData\chocolatey\lib\snoop.2.8.0\tools\snoop.exe");
			var type = assembly.GetType("Snoop.SnoopUI");
			type.GetMethod("GoBabyGo", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}

		public IResult ShowDebug()
		{
			return new WindowResult(Debug);
		}

		public void Touch()
		{
			var src = typeof(ShellViewModel).Assembly.Location;
			var dir = Path.GetDirectoryName(src);
			var filename = Path.GetFileName(src);
			new FileInfo(Path.GetFullPath(Path.Combine(dir, "..", "debug",  filename))).LastWriteTime = DateTime.Now;
		}
#endif
		public void Dispose()
		{
			IsNotifying = false;

			var view = GetView();
			var tabs = ((ShellView) view)?.Items;
			if (tabs != null) {
				foreach (var item in tabs.Items) {
					var el = tabs.ItemContainerGenerator.ContainerFromItem(item) as Control;
					if (el != null)
						el.Template = null;
				}
			}
			if (Items.Count > 0) {
				var items = Items.ToArray();
				Items.Clear();
				ActiveItem = null;
				items.OfType<IDisposable>().Each(x => x.Dispose());
			}

			if (session != null) {
				session.Dispose();
				session = null;
			}

			CloseDisposable.Dispose();
		}

		public T GetPersistedValue<T>(string key, T defaultValue)
		{
			return (T)ViewPersister.ConvertJsonValue(PersistentContext.GetValueOrDefault(key, defaultValue), typeof(T));
		}

		public void Deserialize(StreamReader stream)
		{
			IsNotifying = false;
			try {
				var serializer = GetSerializer();
				serializer.Populate(stream, this);
			} finally {
				IsNotifying = true;
			}
		}

		public void Serialize(StreamWriter stream)
		{
			IsNotifying = false;
			try {
				var serializer = GetSerializer();
				serializer.Serialize(stream, this);
			} finally {
				IsNotifying = true;
			}
		}

		private static JsonSerializer GetSerializer()
		{
			var serializer = new JsonSerializer {
				ContractResolver = new NHibernateResolver(),
#if DEBUG
				Formatting = Formatting.Indented
#endif
			};
			serializer.Converters.Add(new NotifyValueConvert());
			return serializer;
		}
	}
}