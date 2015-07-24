using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		private Address address;
		private IList<WaybillSettings> waybillConfig;
		private static string lastTab;
		private string password;
		private bool passwordUpdated;
		private string diadokPassword;
		private bool diadokPasswordUpdated;

		public bool IsCredentialsChanged;

		public SettingsViewModel()
		{
			SelectedTab = new NotifyValue<string>(lastTab ?? "OverMarkupsTab");
			CurrentWaybillSettings = new NotifyValue<WaybillSettings>();
			CurrentDirMap = new NotifyValue<DirMap>();
			IsWaybillDirEnabled = new NotifyValue<bool>();
			DirMaps = new List<DirMap>();
			Addresses = new List<Address>();
			Styles = new List<CustomStyle>();
			DisplayName = "Настройка";

			if (String.IsNullOrEmpty(Settings.Value.WaybillDir))
				Settings.Value.WaybillDir = Settings.Value.MapPath("Waybills");
			if (String.IsNullOrEmpty(Settings.Value.RejectDir))
				Settings.Value.RejectDir = Settings.Value.MapPath("Rejects");
			if (String.IsNullOrEmpty(Settings.Value.ReportDir))
				Settings.Value.ReportDir = Settings.Value.MapPath("Reports");

			password = Mask(Settings.Value.Password);
			diadokPassword = Mask(Settings.Value.DiadokPassword);

			waybillConfig = Settings.Value.Waybills;
			if (Session != null) {
				Session.FlushMode =  FlushMode.Never;
				Addresses = Session.Query<Address>().OrderBy(a => a.Name).ToList();
				CurrentAddress = Addresses.FirstOrDefault();
				DirMaps = Session.Query<DirMap>().Where(m => m.Supplier.Name != null).OrderBy(d => d.Supplier.FullName).ToList();
				CurrentDirMap.Value = DirMaps.FirstOrDefault();

				var styles = Session.Query<CustomStyle>().OrderBy(s => s.Description).ToList();
				var newStyles = StyleHelper.GetDefaultStyles().Except(styles);
				Session.SaveEach(newStyles);
				Styles = Session.Query<CustomStyle>().OrderBy(s => s.Description).ToList();
			}

			Markups = Settings.Value.Markups.Where(t => t.Type == MarkupType.Over)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Value.Markups, i => Settings.Value.AddMarkup((MarkupConfig)i));
			MarkupConfig.Validate(Markups);

			VitallyImportantMarkups = Settings.Value.Markups
				.Where(t => t.Type == MarkupType.VitallyImportant)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Value.Markups, i => Settings.Value.AddMarkup((MarkupConfig)i));
			MarkupConfig.Validate(VitallyImportantMarkups);

			if (string.IsNullOrEmpty(Settings.Value.UserName))
				SelectedTab.Value = "LoginTab";

			SelectedTab.Subscribe(_ => lastTab = SelectedTab.Value);
			Settings.Value.ObservableForProperty(x => x.GroupWaybillsBySupplier, skipInitial: false)
				.Select(x => !x.Value)
				.Subscribe(IsWaybillDirEnabled);
		}

		public NotifyValue<bool> IsWaybillDirEnabled { get; set; }

		public string Password
		{
			get { return password; }
			set
			{
				passwordUpdated = true;
				password = value;
			}
		}

		public string DiadokPassword
		{
			get { return diadokPassword; }
			set
			{
				diadokPasswordUpdated = true;
				diadokPassword = value;
			}
		}

		public List<DirMap> DirMaps { get; set; }
		public NotifyValue<DirMap> CurrentDirMap { get; set; }

		public NotifyValue<string> SelectedTab { get; set; }

		public IList<MarkupConfig> Markups { get; set; }

		public IList<MarkupConfig> VitallyImportantMarkups { get; set; }

		public List<Address> Addresses { get; set; }

		public List<CustomStyle> Styles { get; set; }

		public Address CurrentAddress
		{
			get
			{
				return address;
			}
			set
			{
				address = value;
				CurrentWaybillSettings.Value = waybillConfig.FirstOrDefault(c => c.BelongsToAddress == value);
			}
		}

		public NotifyValue<WaybillSettings> CurrentWaybillSettings { get; set; }

		private static string Mask(string password1)
		{
			return new string(Enumerable.Repeat('*', (password1 ?? "").Length).ToArray());
		}

		public void NewVitallyImportantMarkup(InitializingNewItemEventArgs e)
		{
			((MarkupConfig)e.NewItem).Type = MarkupType.VitallyImportant;
		}

		public IEnumerable<IResult> SelectWaybillDir()
		{
			var dir = Settings.Value.WaybillDir ?? Settings.Value.MapPath("Waybills");
			if (!Directory.Exists(dir))
				FileHelper.CreateDirectoryRecursive(dir);

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			Settings.Value.WaybillDir = dialog.Result;
		}

		public IEnumerable<IResult> SelectRejectDir()
		{
			var dir = Settings.Value.RejectDir ?? Settings.Value.MapPath("Rejects");
			if (!Directory.Exists(dir))
				FileHelper.CreateDirectoryRecursive(dir);

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			Settings.Value.RejectDir = dialog.Result;
		}

		public IEnumerable<IResult> SelectReportDir()
		{
			var dir = Settings.Value.ReportDir ?? Settings.Value.MapPath("Reports");
			if (!Directory.Exists(dir))
				FileHelper.CreateDirectoryRecursive(dir);

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			Settings.Value.ReportDir = dialog.Result;
		}

		public IEnumerable<IResult> SelectDir()
		{
			if (CurrentDirMap.Value == null)
				yield break;

			var dir = CurrentDirMap.Value.Dir;
			if (!Directory.Exists(dir))
				FileHelper.CreateDirectoryRecursive(dir);

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			CurrentDirMap.Value.Dir = dialog.Result;
		}

		public void Save()
		{
			var error = Settings.Value.ValidateMarkups();

			if (!String.IsNullOrEmpty(error)) {
				Session.FlushMode = FlushMode.Never;
				Manager.Warning(error);
				return;
			}

			if (passwordUpdated)
				Settings.Value.Password = password;
			if (diadokPasswordUpdated)
				Settings.Value.DiadokPassword = diadokPassword;

			if (App.Current != null)
				StyleHelper.BuildStyles(App.Current.Resources, Styles);

			IsCredentialsChanged = Session.IsChanged(Settings.Value, s => s.Password)
				|| Session.IsChanged(Settings.Value, s => s.UserName);
			if (Session.IsChanged(Settings.Value, s => s.GroupWaybillsBySupplier)
				&& Settings.Value.GroupWaybillsBySupplier) {
				foreach (var dirMap in DirMaps) {
					try {
						if (!Directory.Exists(dirMap.Dir))
							FileHelper.CreateDirectoryRecursive(dirMap.Dir);
					}
					catch(Exception e) {
						Log.Error(String.Format("Не удалось создать директорию {0}", dirMap.Dir), e);
					}
				}
			}

			Session.FlushMode = FlushMode.Auto;
			Settings.Value.ApplyChanges(Session);
			TryClose();
		}

		protected override void Broadcast()
		{
			Bus.SendMessage<Settings>(null);
		}

		public IEnumerable<IResult> EditColor(CustomStyle style)
		{
			return CustomStyle.Edit(style);
		}
	}
}