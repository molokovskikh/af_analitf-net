using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
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

	public class ColumnSettings
	{
		public ColumnSettings()
		{
		}

		public ColumnSettings(DataGridColumn dataGridColumn)
		{
			Name = dataGridColumn.Header.ToString();
			Visible = dataGridColumn.Visibility;
			Width = dataGridColumn.Width;
			DisplayIndex = dataGridColumn.DisplayIndex;
		}

		public void Restore(DataGridColumn column)
		{
			column.Width = Width;
			column.Visibility = Visible;
			//безумие, если колонке два раза сказать что у нее DisplayIndex -1, то будет исключение
			//ArgumentOutOfRangeException
			if (column.DisplayIndex != DisplayIndex)
				column.DisplayIndex = DisplayIndex;
		}

		public string Name;
		public Visibility Visible;
		public DataGridLength Width;
		public int DisplayIndex;

		public override string ToString()
		{
			return Name;
		}
	}

	[DataContract]
	public class ShellViewModel : Conductor<IScreen>
	{
		private Stack<IScreen> navigationStack = new Stack<IScreen>();
		private Settings settings;
		private WindowManager windowManager;
		private ISession session;
		private ILog log = LogManager.GetLogger(typeof(ShellViewModel));
		private List<Address> addresses;
		private Address currentAddress;

		public ShellViewModel()
		{
			ViewSettings = new Dictionary<string, List<ColumnSettings>>();
			Arguments = Environment.GetCommandLineArgs();

			var factory = AppBootstrapper.NHibernate.Factory;
			session = factory.OpenSession();

			Reload();
			windowManager = (WindowManager)IoC.Get<IWindowManager>();

			DisplayName = "АналитФАРМАЦИЯ";
			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => UpdateDisplayName());

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => NotifyOfPropertyChange("CanPrint"));

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => NotifyOfPropertyChange("CanExport"));

			this.ObservableForProperty(m => m.CanPrint)
				.Subscribe(_ => NotifyOfPropertyChange("CanPrintPreview"));
		}

		public string[] Arguments;

		[DataMember]
		public Dictionary<string, List<ColumnSettings>> ViewSettings;

		protected override void OnViewLoaded(object view)
		{
			var import = Arguments.LastOrDefault().Match("import");
			if (import) {
				Import();
			}
			else {
				CheckSettings();
			}
		}

		public void OnLoaded()
		{
			OnViewLoaded(GetView());
		}

		public List<Address> Addresses
		{
			get { return addresses; }
			set
			{
				addresses = value;
				NotifyOfPropertyChange("Addresses");
			}
		}

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

		private bool CheckSettings()
		{
			Reload();
			if (!settings.IsValid) {
				windowManager.Warning("Для начала работы с программой необходимо заполнить учетные данные");
				ShowSettings();
				return false;
			}
			return true;
		}

		public void Reload()
		{
			session.Clear();

			settings = session.Query<Settings>().First();
			Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.FirstOrDefault();
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

		public void ShowCatalog()
		{
			ActivateRootItem(new CatalogViewModel());
		}

		public void ShowPrice()
		{
			ActivateRootItem(new PriceViewModel());
		}

		public void ShowMnn()
		{
			ActivateRootItem(new MnnViewModel());
		}

		public void SearchOffers()
		{
			ActivateRootItem(new SearchOfferViewModel());
		}

		public void ShowSettings()
		{
			windowManager.ShowFixedDialog(new SettingsViewModel());
		}

		public void ShowOrderLines()
		{
			ActivateRootItem(new OrderLinesViewModel());
		}

		public void ShowJunkOffers()
		{
			ActivateRootItem(new JunkOfferViewModel());
		}

		public void ShowOrders()
		{
			if (ActiveItem is CatalogOfferViewModel)
				Navigate(new OrdersViewModel());
			else
				ActivateRootItem(new OrdersViewModel());
		}

		private void ActivateRootItem(IScreen screen)
		{
			if (ActiveItem != null && ActiveItem.GetType() == screen.GetType())
				return;

			var toClose = navigationStack.TakeWhile(s => s.GetType() != screen.GetType());
			foreach (var closing in toClose) {
				closing.TryClose();
			}

			if (ActiveItem != null)
				ActiveItem.TryClose();

			if (ActiveItem == null)
				ActivateItem(screen);
		}

		public void Update()
		{
			Sync("Обновление завершено успешно.",
				"Не удалось получить обновление. Попробуйте повторить операцию позднее.",
				Tasks.Update);
		}

		public bool CanSendOrders
		{
			get { return true; }
		}

		public void SendOrders()
		{
			Sync("Отправка заказов завершена успешно.",
				"Не удалось отправить заказы. Попробуйте повторить операцию позднее.",
				Tasks.SendOrders);
		}

		private void Import()
		{
			Sync("Обновление завершено успешно.",
				"Не удалось получить обновление. Попробуйте повторить операцию позднее.",
				Tasks.Import);
		}

		public void CheckDb()
		{
			RunTask(
				new WaitViewModel("Производится восстановление базы данных.\r\nПодождите..."),
				Tasks.CheckAndRepairDb,
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
			var result = windowManager.Question("При создании базы данных будут потеряны текущие заказы.\r\nПродолжить?");
			if (result != MessageBoxResult.Yes)
				return;

			RunTask(
				new WaitViewModel("Производится очистка базы данных.\r\nПодождите..."),
				Tasks.CleanDb,
				t => Update());
			Reload();
		}

		private void Sync(string sucessMessage, string errorMessage, Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, UpdateResult> func)
		{
			if (!CheckSettings())
				return;

			var progress = new BehaviorSubject<Progress>(new Progress());
			var wait = new SyncViewModel(progress) {
				GenericErrorMessage = errorMessage
			};
			var credential = new NetworkCredential(settings.UserName, settings.Password);

			RunTask(wait,
				t => func(credential, t, progress),
				t => {
					if (t.Result == UpdateResult.UpdatePending) {
						RunUpdate();
					}
					else {
						windowManager.Notify(sucessMessage);
					}
				});
			Reload();
		}

		private void RunUpdate()
		{
			windowManager.Warning("Получена новая версия программы. Сейчас будет выполено обновление.");
			var updateExePath = Path.Combine(AppBootstrapper.TempPath, "update", "Updater.exe");
			Process.Start(updateExePath, Process.GetCurrentProcess().Id.ToString());
			TryClose();
		}

		private string TranslateException(AggregateException exception)
		{
			var requestException = exception.GetBaseException() as RequestException;
			if (requestException != null) {
				if (requestException.StatusCode == HttpStatusCode.Unauthorized) {
					return "Доступ запрещен.\r\nВведены некорректные учетные данные.";
				}
				if (requestException.StatusCode == HttpStatusCode.Forbidden) {
					return "Доступ запрещен.\r\nОбратитесь в АК Инфорум.";
				}
			}
			return null;
		}

		private Task<T> RunTask<T>(WaitViewModel viewModel, Task<T> task, Action<Task<T>> continueWith)
		{
			ResetNavigation();

			task.ContinueWith(t => {
				viewModel.IsCompleted = true;
				viewModel.TryClose();
			}, TaskScheduler.FromCurrentSynchronizationContext());
			task.Start();

			windowManager.ShowFixedDialog(viewModel);

			if (!task.IsCanceled && !task.IsFaulted) {
				continueWith(task);
			}
			else if (task.IsFaulted) {
				log.Error(task.Exception);
				var error = TranslateException(task.Exception)
					?? viewModel.GenericErrorMessage;
				windowManager.Error(error);
			}
			return task;
		}

		private void RunTask<T>(WaitViewModel viewModel, Func<CancellationToken, T> func, Action<Task<T>> success)
		{
			var token = viewModel.Cancellation.Token;
			var task = new Task<T>(() => func(token), token);
			RunTask(viewModel, task, success);
		}

		private void RunTask(WaitViewModel viewModel, Action<CancellationToken> action, Action<Task> success)
		{
			var token = viewModel.Cancellation.Token;
			var task = new Task<object>(() => {
				action(token);
				return null;
			}, token);
			RunTask(viewModel, task, success);
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

#if DEBUG
		public void Snoop()
		{
			var assembly = Assembly.Load("snoop");
			var type = assembly.GetType("Snoop.SnoopUI");
			type.GetMethod("GoBabyGo", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}
#endif
	}
}