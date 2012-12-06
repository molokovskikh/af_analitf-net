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
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using LogManager = Caliburn.Micro.LogManager;

namespace AnalitF.Net.Client.ViewModels
{
	public interface IPrintable
	{
		bool CanPrint { get; }

		void Print();
	}

	[Serializable]
	public class ShellViewModel : Conductor<IScreen>
	{
		private Stack<IScreen> navigationStack = new Stack<IScreen>();
		private Settings settings;
		private Extentions.WindowManager windowManager;
		private ISession session;
		private ILog log = LogManager.GetLog(typeof(ShellViewModel));
		private List<Address> addresses;
		private Address currentAddress;

		public ShellViewModel()
		{
			AppBootstrapper.Shell = this;

			var factory = AppBootstrapper.NHibernate.Factory;
			session = factory.OpenSession();

			windowManager = (Extentions.WindowManager)IoC.Get<IWindowManager>();

			settings = session.Query<Settings>().First();
			Addresses = session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.FirstOrDefault();

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

		protected override void OnActivate()
		{
			base.OnActivate();

			((Window)GetView()).Loaded += (sender, args) => {
				IsSettingsValid();
			};
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

		private bool IsSettingsValid()
		{
			session.Clear();
			settings = session.Query<Settings>().First();
			if (!settings.IsValid) {
				windowManager.Warning("Для начала работы с программой необходимо заполнить учетные данные");
				ShowSettings();
				return false;
			}
			return true;
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

		public void Print()
		{
			if (!CanPrint)
				return;

			((IPrintable)ActiveItem).Print();
		}

		public bool CanPrintPreview
		{
			get { return CanPrint; }
		}

		public void PrintPreview()
		{

		}

		public void ShowCatalog()
		{
			ResetNavigation();
			ActivateItem(new CatalogViewModel());
		}

		public void ShowPrice()
		{
			ResetNavigation();
			ActivateItem(new PriceViewModel());
		}

		public void ShowMnn()
		{
			ResetNavigation();
			ActivateItem(new MnnViewModel());
		}

		public void SearchOffers()
		{
			ResetNavigation();
			ActivateItem(new SearchOfferViewModel());
		}

		public void ShowSettings()
		{
			windowManager.ShowFixedDialog(new SettingsViewModel());
		}

		public void ShowOrderLines()
		{
			ResetNavigation();
			ActivateItem(new OrderLinesViewModel());
		}

		public void ShowJunkOffers()
		{
			ResetNavigation();
			ActivateItem(new JunkOfferViewModel());
		}

		public void ShowOrders()
		{
			ResetNavigation();
			ActivateItem(new OrdersViewModel());
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

		private void Sync(string sucessMessage, string errorMessage, Func<ICredentials, CancellationToken, BehaviorSubject<Progress>, Task<UpdateResult>> func)
		{
			if (!IsSettingsValid())
				return;

			ResetNavigation();

			var cancellation = new CancellationTokenSource();
			var token = cancellation.Token;
			var progress = new BehaviorSubject<Progress>(new Progress());
			var credential = new NetworkCredential(settings.UserName, settings.Password);
			var task = func(credential, token, progress);

			var wait = new WaitCancelViewModel(cancellation, progress);
			task.ContinueWith(t => {
				wait.IsCompleted = true;
				wait.TryClose();
				if (!t.IsFaulted && !t.IsCanceled) {
					if (t.Result == UpdateResult.UpdatePending) {
						RunUpdate();
					}
					else {
						windowManager.Notify(sucessMessage);
					}
				}
				else if (t.IsFaulted) {
					log.Error(t.Exception);
					var error = TranslateException(t.Exception)
						?? errorMessage;
					windowManager.Error(error);
				}
			},
				TaskScheduler.FromCurrentSynchronizationContext());
			task.Start();

			windowManager.ShowFixedDialog(wait);
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

		private void ResetNavigation()
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

			if (ActiveItem == null && navigationStack.Count > 0) {
				ActivateItem(navigationStack.Peek());
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