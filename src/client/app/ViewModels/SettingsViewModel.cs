using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Iesi.Collections;
using NHibernate;
using NHibernate.Linq;
using Color = System.Drawing.Color;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		private Address address;
		private IList<WaybillSettings> waybillConfig;
		private static string lastTab;
		public bool IsCredentialsChanged;
		private bool _passwordUpdated;
		private string _password;

		public SettingsViewModel()
		{
			SelectedTab = new NotifyValue<string>(lastTab ?? "OverMarkupsTab");
			CurrentWaybillSettings = new NotifyValue<WaybillSettings>();
			CurrentDirMap = new NotifyValue<DirMap>();

			Session.FlushMode =  FlushMode.Never;
			DisplayName = "Настройка";

			_password = new string(Enumerable.Repeat('*', Settings.Value.Password != null ? Settings.Value.Password.Length : 0).ToArray());

			waybillConfig = Settings.Value.Waybills;
			Addresses = Session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.FirstOrDefault();
			DirMaps = Session.Query<DirMap>().Where(m => m.Supplier.Name != null).OrderBy(d => d.Supplier.FullName).ToList();
			CurrentDirMap.Value = DirMaps.FirstOrDefault();

			var styles = Session.Query<CustomStyle>().OrderBy(s => s.Description).ToList();
			var newStyles = StyleHelper.GetDefaultStyles().Except(styles);
			Session.SaveEach(newStyles);
			Styles = Session.Query<CustomStyle>().OrderBy(s => s.Description).ToList();

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

			SelectedTab.Changed().Subscribe(_ => lastTab = SelectedTab.Value);
		}

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
			var error = Settings.Value.ValidateMarkups();

			if (!String.IsNullOrEmpty(error)) {
				Session.FlushMode = FlushMode.Never;
				Manager.Warning(error);
				return;
			}

			if (_passwordUpdated) {
				Settings.Value.Password = _password;
			}

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
						log.Error(String.Format("Не удалось создать директорию {0}", dirMap.Dir), e);
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