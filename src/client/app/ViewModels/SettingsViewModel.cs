using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Iesi.Collections;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		private Address address;
		private IList<WaybillSettings> waybillConfig;
		private static string lastTab;
		public bool IsCredentialsChanged;

		public SettingsViewModel()
		{
			SelectedTab = new NotifyValue<string>(lastTab ?? "OverMarkupsTab");
			CurrentWaybillSettings = new NotifyValue<WaybillSettings>();
			CurrentDirMap = new NotifyValue<DirMap>();

			Session.FlushMode =  FlushMode.Never;
			DisplayName = "Настройка";

			Settings = Session.Query<Settings>().First();
			waybillConfig = Settings.Waybills;
			Addresses = Session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.FirstOrDefault();
			DirMaps = Session.Query<DirMap>().Where(m => m.Supplier.Name != null).OrderBy(d => d.Supplier.FullName).ToList();
			CurrentDirMap.Value = DirMaps.FirstOrDefault();

			Markups = Settings.Markups.Where(t => t.Type == MarkupType.Over)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Markups, i => Settings.AddMarkup((MarkupConfig)i));
			MarkupConfig.Validate(Markups);

			VitallyImportantMarkups = Settings.Markups
				.Where(t => t.Type == MarkupType.VitallyImportant)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Markups, i => Settings.AddMarkup((MarkupConfig)i));
			MarkupConfig.Validate(VitallyImportantMarkups);

			if (string.IsNullOrEmpty(Settings.UserName))
				SelectedTab.Value = "LoginTab";

			SelectedTab.Changed().Subscribe(_ => lastTab = SelectedTab.Value);
		}

		public List<DirMap> DirMaps { get; set; }
		public NotifyValue<DirMap> CurrentDirMap { get; set; }

		public NotifyValue<string> SelectedTab { get; set; }

		public new Settings Settings { get; set; }

		public IList<MarkupConfig> Markups { get; set; }

		public IList<MarkupConfig> VitallyImportantMarkups { get; set; }

		public List<Address> Addresses { get; set; }

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

		public void NewVitallyImportantMarkup(InitializingNewItemEventArgs e)
		{
			((MarkupConfig)e.NewItem).Type = MarkupType.VitallyImportant;
		}

		public IEnumerable<IResult> SelectDir()
		{
			if (CurrentDirMap.Value == null)
				yield break;

			if (!Directory.Exists(CurrentDirMap.Value.Dir))
				FileHelper.CreateDirectoryRecursive(CurrentDirMap.Value.Dir);

			var dialog = new SelectDirResult(CurrentDirMap.Value.Dir);
			yield return dialog;
			CurrentDirMap.Value.Dir = dialog.Result;
		}

		public void Save()
		{
			var error = Settings.ValidateMarkups();

			if (!String.IsNullOrEmpty(error)) {
				Session.FlushMode = FlushMode.Never;
				Manager.Warning(error);
				return;
			}

			IsCredentialsChanged = Session.IsChanged(Settings, s => s.Password)
				|| Session.IsChanged(Settings, s => s.UserName);

			Session.FlushMode = FlushMode.Auto;
			Settings.ApplyChanges(Session);
			TryClose();
		}

		protected override void Broadcast()
		{
			Bus.SendMessage<Settings>(null);
		}
	}
}