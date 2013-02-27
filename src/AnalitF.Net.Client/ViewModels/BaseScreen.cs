using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
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
	public class BaseScreen : Screen, IActivateEx, IExportable
	{
		private Dictionary<string, List<ColumnSettings>> viewDefaults
			= new Dictionary<string, List<ColumnSettings>>();

		protected ILog log;

		protected virtual ShellViewModel Shell
		{
			get { return ((ShellViewModel)Parent); }
		}

		protected ExcelExporter excelExporter;

		public Extentions.WindowManager Manager { get; private set; }

		protected bool FlushOnClose = true;
		protected ISession Session;
		protected IStatelessSession StatelessSession;

		protected Settings Settings;
		protected Address Address;

		public static IScheduler TestSchuduler;
		public IScheduler Scheduler = TestSchuduler ?? DefaultScheduler.Instance;
		public IScheduler UiScheduler = TestSchuduler ?? DispatcherScheduler.Current;
		protected IMessageBus Bus = RxApp.MessageBus;

		public BaseScreen()
		{
			log = log4net.LogManager.GetLogger(GetType());
			var factory = AppBootstrapper.NHibernate.Factory;
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();

			StatelessSession = factory.OpenStatelessSession();
			Session = factory.OpenSession();
			Session.BeginTransaction();

			Settings = Session.Query<Settings>().First();
			User = Session.Query<User>().FirstOrDefault();

			excelExporter = new ExcelExporter(this);
		}

		public bool IsSuccessfulActivated { get; protected set; }

		public User User { get; private set; }

		public bool CanExport
		{
			get { return excelExporter.CanExport; }
		}

		public virtual void NavigateBackward()
		{
			var canClose = Shell == null || Shell.NavigationStack.Any();
			if (canClose)
				TryClose();
		}

		protected override void OnInitialize()
		{
			Load();
			Restore();
		}

		private void Load()
		{
			if (Shell == null)
				return;

			if (Shell.CurrentAddress != null)
				Address = Session.Load<Address>(Shell.CurrentAddress.Id);
		}

		private void Restore()
		{
			if (Shell == null)
				return;

			var key = GetType().FullName;
			if (Shell.ViewModelSettings.ContainsKey(key)) {
				try {
					IsNotifying = false;
					JsonConvert.PopulateObject(Shell.ViewModelSettings[key], this, SerializerSettings());
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

		//метод нужен для того что бы форма могла изменять
		//ActiveItem тк делать это в OnActivate нельзя
		//например открыть дочерний елемент если он один
		public virtual void PostActivated()
		{
		}

		protected override void OnActivate()
		{
			IsSuccessfulActivated = true;
		}

		protected override void OnDeactivate(bool close)
		{
			if (FlushOnClose) {
				if (Session.Transaction.IsActive)
					Session.Transaction.Commit();

				Session.Flush();
			}

			if (close) {
				Save();
				SaveView(GetView());
			}
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
			var json = JsonConvert.SerializeObject(this, SerializerSettings());
			Shell.ViewModelSettings.Add(key, json);
		}

		private static JsonSerializerSettings SerializerSettings()
		{
			var settings = new JsonSerializerSettings {
				ContractResolver = new NHibernateResolver()
			};
			return settings;
		}

		private void SaveView(object view)
		{
			if (view == null)
				return;

			foreach (var grid in GetControls(view)) {
				SaveView(grid, Shell.ViewSettings);
			}
		}

		private void SaveView(DataGrid grid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var key = GetViewKey(grid);
			if (storage.ContainsKey(key)) {
				storage.Remove(key);
			}
			storage.Add(key, grid.Columns.Select((c, i) => new ColumnSettings(c, i)).ToList());
		}

		protected void RestoreView(object view)
		{
			foreach (var grid in GetControls(view)) {
				SaveView(grid, viewDefaults);
				RestoreView(grid, Shell.ViewSettings);
			}
		}

		public void ResetView(DataGrid grid)
		{
			RestoreView(grid, viewDefaults);
		}

		private void RestoreView(DataGrid dataGrid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var key = GetViewKey(dataGrid);
			if (!storage.ContainsKey(key))
				return;

			var settings = storage[key];
			if (settings == null)
				return;

			foreach (var setting in settings) {
				var column = dataGrid.Columns.FirstOrDefault(c => c.Header.Equals(setting.Name));
				if (column == null)
					return;
				setting.Restore(column);
			}
		}

		private string GetViewKey(DataGrid grid)
		{
			return GetType().Name + "." + grid.Name;
		}

		public IEnumerable<DataGrid> GetControls(object view)
		{
			var dependencyObject = view as DependencyObject;
			if (dependencyObject == null || Shell == null)
				return Enumerable.Empty<DataGrid>();
			return dependencyObject.DeepChildren()
				.OfType<DataGrid>()
				.Where(c => (bool)c.GetValue(ContextMenuBehavior.PersistColumnSettingsProperty));
		}

		protected override void OnViewAttached(object view, object context)
		{
			RestoreView(view);
		}

		public override string ToString()
		{
			return DisplayName;
		}

		public IResult Export()
		{
			return excelExporter.Export();
		}

		protected bool Confirm(string message)
		{
			return Manager.Question(message) == MessageBoxResult.Yes;
		}
	}
}