﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
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
		private bool _passwordUpdated;
		private string _password;

		public bool IsCredentialsChanged;

		public SettingsViewModel()
		{
			MarkupAddress = new NotifyValue<Address>();
			Markups = new NotifyValue<IList<MarkupConfig>>();
			VitallyImportantMarkups = new NotifyValue<IList<MarkupConfig>>();
			Nds18Markups = new NotifyValue<IList<MarkupConfig>>();

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

			_password = new string(Enumerable.Repeat('*', Settings.Value.Password?.Length ?? 0).ToArray());
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
			else {
				Addresses = Env.Addresses;
			}

			MarkupAddress.Select(x => MarkupByType(MarkupType.Over, x))
				.Subscribe(Markups);
			MarkupAddress.Select(x => MarkupByType(MarkupType.VitallyImportant, x))
				.Subscribe(VitallyImportantMarkups);
			MarkupAddress.Select(x => MarkupByType(MarkupType.Nds18, x))
				.Subscribe(Nds18Markups);
			MarkupAddress.Value = Addresses.FirstOrDefault();

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
			get { return _password; }
			set
			{
				_passwordUpdated = true;
				_password = value;
			}
		}

		public List<DirMap> DirMaps { get; set; }
		public NotifyValue<DirMap> CurrentDirMap { get; set; }

		public NotifyValue<string> SelectedTab { get; set; }

		public NotifyValue<Address> MarkupAddress { get; set; }
		public bool OverwriteNds18Markups { get; set; }
		public NotifyValue<IList<MarkupConfig>> Nds18Markups { get; set; }
		public bool OverwriteMarkups { get; set; }
		public NotifyValue<IList<MarkupConfig>> Markups { get; set; }
		public bool OverwriteVitallyImportant { get; set; }
		public NotifyValue<IList<MarkupConfig>> VitallyImportantMarkups { get; set; }

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

		public IList<MarkupConfig> MarkupByType(MarkupType type, Address address)
		{
			var result = Settings.Value.Markups
				.Where(t => t.Type == type && t.Address == address)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Value.Markups, i => Settings.Value.AddMarkup((MarkupConfig)i));
			MarkupConfig.Validate(result);
			return result;
		}

		public void NewVitallyImportantMarkup(InitializingNewItemEventArgs e)
		{
			var markup = ((MarkupConfig)e.NewItem);
			markup.Type = MarkupType.VitallyImportant;
			markup.Address = MarkupAddress.Value;
		}

		public void NewNds18Markup(InitializingNewItemEventArgs e)
		{
			var markup = ((MarkupConfig)e.NewItem);
			markup.Type = MarkupType.Nds18;
			markup.Address = MarkupAddress.Value;
		}

		public void NewMarkup(InitializingNewItemEventArgs e)
		{
			var markup = ((MarkupConfig)e.NewItem);
			markup.Address = MarkupAddress.Value;
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

		public void UpdateMarkups()
		{
			var markups = Settings.Value.Markups;
			var dstAddresses = Addresses.Where(x => x != MarkupAddress.Value).ToArray();
			if (OverwriteMarkups) {
				var items = markups.Where(x => x.Type == MarkupType.Over).ToArray();
				var src = items.Where(x => x.Address == MarkupAddress.Value).ToArray();
				markups.RemoveEach(items.Except(src));
				markups.AddEach(dstAddresses.SelectMany(x => src.Select(y => new MarkupConfig(y, x))));
			}
			if (OverwriteNds18Markups) {
				var items = markups.Where(x => x.Type == MarkupType.Nds18).ToArray();
				var src = items.Where(x => x.Address == MarkupAddress.Value).ToArray();
				markups.RemoveEach(items.Except(src));
				markups.AddEach(dstAddresses.SelectMany(x => src.Select(y => new MarkupConfig(y, x))));
			}
			if (OverwriteVitallyImportant) {
				var items = markups.Where(x => x.Type == MarkupType.VitallyImportant).ToArray();
				var src = items.Where(x => x.Address == MarkupAddress.Value).ToArray();
				markups.RemoveEach(items.Except(src));
				markups.AddEach(dstAddresses.SelectMany(x => src.Select(y => new MarkupConfig(y, x))));
			}
		}

		public IEnumerable<IResult> Save()
		{
			var error = Settings.Value.Validate();

			if (!String.IsNullOrEmpty(error)) {
				Session.FlushMode = FlushMode.Never;
				Manager.Warning(error);
				yield break;
			}

			UpdateMarkups();
			if (_passwordUpdated)
				Settings.Value.Password = _password;

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

			if (Session.IsChanged(Settings.Value, x => x.JunkPeriod))
				yield return new Models.Results.TaskResult(TplQuery(s => DbMaintain.CalcJunk(s, Settings.Value)));

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