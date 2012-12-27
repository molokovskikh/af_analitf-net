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
	public class BaseScreen : Screen
	{
		protected ILog log;

		protected ShellViewModel Shell
		{
			get { return ((ShellViewModel)Parent); }
		}

		protected Extentions.WindowManager Manager { get; private set; }

		protected ISession Session;
		protected IStatelessSession StatelessSession;

		protected Settings Settings;
		protected Address Address;

		public static IScheduler Scheduler = DefaultScheduler.Instance;

		public BaseScreen()
		{
			log = log4net.LogManager.GetLogger(GetType());
			var factory = AppBootstrapper.NHibernate.Factory;
			StatelessSession = factory.OpenStatelessSession();
			Session = factory.OpenSession();
			Settings = Session.Query<Settings>().First();
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
		}

		public virtual void NavigateBackward()
		{
			var canClose = Shell == null || Shell.NavigationStack.Any();
			if (canClose)
				TryClose();
		}

		protected override void OnInitialize()
		{
			if (Shell == null)
				return;

			if (Shell.CurrentAddress != null) {
				Address = Session.Load<Address>(Shell.CurrentAddress.Id);
			}

			Restore();
		}

		private void Restore()
		{
			var key = GetType().FullName;
			if (Shell.ViewModelSettings.ContainsKey(key)) {
				try {
					JsonConvert.PopulateObject(Shell.ViewModelSettings[key], this);
				}
				catch (Exception e) {
					log.Error(String.Format("Не удалось прочитать настройки, для {0}", GetType()), e);
				}
				finally {
					Shell.ViewModelSettings.Remove(key);
				}
			}
		}

		protected override void OnDeactivate(bool close)
		{
			Session.Flush();

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
			Shell.ViewModelSettings.Add(key, JsonConvert.SerializeObject(this));
		}

		private void SaveView(object view)
		{
			if (view == null)
				return;

			foreach (var grid in GetControls(view)) {
				var key = GetViewKey(grid);
				if (Shell.ViewSettings.ContainsKey(key)) {
					Shell.ViewSettings.Remove(key);
				}
				Shell.ViewSettings.Add(key, grid.Columns.Select(c => new ColumnSettings(c)).ToList());
			}
		}

		protected void RestoreView(object view)
		{
			foreach (var dataGrid in GetControls(view)) {
				var key = GetViewKey(dataGrid);
				if (!Shell.ViewSettings.ContainsKey(key))
					continue;

				var settings = Shell.ViewSettings[key];
				if (settings == null)
					return;

				foreach (var setting in settings) {
					var column = dataGrid.Columns.FirstOrDefault(c => c.Header.Equals(setting.Name));
					if (column == null)
						return;
					setting.Restore(column);
				}
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
				.Where(c => c.Name == "Offers");
		}

		protected override void OnViewAttached(object view, object context)
		{
			RestoreView(view);
		}

		public override string ToString()
		{
			return DisplayName;
		}
	}
}