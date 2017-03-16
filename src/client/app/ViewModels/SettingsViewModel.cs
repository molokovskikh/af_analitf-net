using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Dapper;
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
		private string sbisPassword;
		private bool sbisPasswordUpdated;

		public bool IsCredentialsChanged;

		public List<int> LastDayForWarnOrdered { get; set; }

		public SettingsViewModel()
		{
			InitFields();

			DisplayName = "Настройка";
			SelectedTab.Value = lastTab ?? "OverMarkupsTab";

			if (String.IsNullOrEmpty(Settings.Value.WaybillDir))
				Settings.Value.WaybillDir = Settings.Value.MapPath("Waybills");
			if (String.IsNullOrEmpty(Settings.Value.RejectDir))
				Settings.Value.RejectDir = Settings.Value.MapPath("Rejects");
			if (String.IsNullOrEmpty(Settings.Value.ReportDir))
				Settings.Value.ReportDir = Settings.Value.MapPath("Reports");

			password = Mask(Settings.Value.Password);
			diadokPassword = Mask(Settings.Value.DiadokPassword);
			sbisPassword = Mask(Settings.Value.SbisPassword);

			waybillConfig = Settings.Value.Waybills;
			if (Session != null) {
				Session.FlushMode = FlushMode.Never;
				DirMaps = Session.Query<DirMap>().Where(m => m.Supplier.Name != null).OrderBy(d => d.Supplier.FullName).ToList();
				CurrentDirMap.Value = DirMaps.FirstOrDefault();

				var styles = Session.Query<CustomStyle>().OrderBy(s => s.Description).ToList();
				var newStyles = StyleHelper.GetDefaultStyles().Except(styles);
				Session.SaveEach(newStyles);
				Styles = Session.Query<CustomStyle>().OrderBy(s => s.Description).ToList();
			}

			HaveAddresses = Addresses.Length > 0;
			MarkupAddress.Select(x => MarkupByType(MarkupType.Over, x))
				.Subscribe(Markups);
			MarkupAddress.Select(x => MarkupByType(MarkupType.VitallyImportant, x))
				.Subscribe(VitallyImportantMarkups);
			MarkupAddress.Select(x => MarkupByType(MarkupType.Nds18, x))
				.Subscribe(Nds18Markups);
			MarkupAddress.Select(x => MarkupByType(MarkupType.Special, x))
				.Subscribe(SpecialMarkups);
			MarkupAddress.Select(x => Settings.Value.PriceTags.FirstOrDefault(r => r.Address == x))
				.Subscribe(PriceTagSettings);
			MarkupAddress.Select(x => Settings.Value.PriceTags.FirstOrDefault(r => r.Address == x))
				.Subscribe(_ => LoadPriceTagPreview());

			SearchBehavior = new SearchBehavior(this);
			IsLoading = new NotifyValue<bool>(true);

			SpecialMarkupSearchText
				.Merge(SpecialMarkupSearchText.Select(v => (object)v))
				.Merge(SpecialMarkupSearchEverywhere.Select(v => (object)v))
				.Merge(SpecialMarkupSearchInStartOfString.Select(v => (object)v))
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Do(_ => IsLoading.Value = true)
				.Select(_ => RxQuery(SearchProduct))
				.Switch()
				.Do(_ => IsLoading.Value = false)
				.Subscribe(Products, CloseCancellation.Token);

			RxQuery(s => s.Query<SpecialMarkupCatalog>().OrderBy(n => n.Name).ToObservableCollection())
				.Subscribe(SpecialMarkupProducts);

			if (string.IsNullOrEmpty(Settings.Value.UserName))
				SelectedTab.Value = "LoginTab";

			SelectedTab.Subscribe(_ => lastTab = SelectedTab.Value);
			Settings.Value.ObservableForProperty(x => x.GroupWaybillsBySupplier, skipInitial: false)
				.Select(x => !x.Value)
				.Subscribe(IsWaybillDirEnabled);

			LastDayForWarnOrdered = new List<int>() {1,2,3,4,5,6,7};
			Printers = PrintHelper.GetPrinters().Select(x => x.Name).ToArray();
			if (Session != null)
				WriteoffReasons = new PersistentList<WriteoffReason>(Session.Query<WriteoffReason>().OrderBy(x => x.Name).ToList(), Session);
		}

		public string[] Printers { get; set; }
		public PersistentList<WriteoffReason> WriteoffReasons { get; set; }
		public bool HaveAddresses { get; set; }
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

		public string SbisPassword
		{
			get { return sbisPassword; }
			set
			{
				sbisPasswordUpdated = true;
				sbisPassword = value;
			}
		}

		public List<DirMap> DirMaps { get; set; }
		public NotifyValue<DirMap> CurrentDirMap { get; set; }

		public NotifyValue<string> SelectedTab { get; set; }

		public NotifyValue<Address> MarkupAddress { get; set; }
		public bool OverwriteNds18Markups { get; set; }
		public NotifyValue<IList<MarkupConfig>> Nds18Markups { get; set; }
		public bool OverwriteSpecialMarkups { get; set; }
		public NotifyValue<IList<MarkupConfig>> SpecialMarkups { get; set; }
		public bool OverwriteMarkups { get; set; }
		public NotifyValue<IList<MarkupConfig>> Markups { get; set; }
		public bool OverwriteVitallyImportant { get; set; }
		public NotifyValue<IList<MarkupConfig>> VitallyImportantMarkups { get; set; }

		public NotifyValue<string> SpecialMarkupSearchText { get; set; }
		public NotifyValue<bool> SpecialMarkupSearchEverywhere { get; set; }
		public NotifyValue<bool> SpecialMarkupSearchInStartOfString { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }
		public SearchBehavior SearchBehavior { get; set; }


		public NotifyValue<List<CatalogDisplayItem>> Products { get; set; }
		public NotifyValue<CatalogDisplayItem> CurrentProduct { get; set; }

		public NotifyValue<ObservableCollection<SpecialMarkupCatalog>> SpecialMarkupProducts { get; set; }
		public NotifyValue<SpecialMarkupCatalog> CurrentSpecialMarkupProduct { get; set; }

		public List<CatalogDisplayItem> SearchProduct(IStatelessSession session)
		{
			var conditions = new List<string>();
			//Если введен текст поиска
			if (!string.IsNullOrEmpty(SpecialMarkupSearchText.Value))
				conditions.Add("(cn.Name like @term or c.Form like @term) ");
			//Если ищем везде
			if (!SpecialMarkupSearchEverywhere)
				conditions.Add("c.HaveOffers = 1 ");
			//Формируем блок условия
			var where = conditions.Count > 0 ? " where " + conditions.Implode(" and ") : "";
			var sql = $@"select c.Id as CatalogId, cn.Name, c.Form, c.HaveOffers, c.VitallyImportant
from Catalogs c
	join CatalogNames cn on cn.Id = c.NameId
{where}
order by cn.Name, c.Form
limit 300";
			return session.Connection
				.Query<CatalogDisplayItem>(sql, new { term = "%" + SpecialMarkupSearchText.Value + "%" })
				.ToList();
		}

		public void SpecialMarkupCheck()
		{
			if (CurrentProduct.Value == null)
				return;
			if (SpecialMarkupProducts.Value.Any(x => x.CatalogId == CurrentProduct.Value.CatalogId))
				return;
			SpecialMarkupProducts.Value.Add(new SpecialMarkupCatalog(CurrentProduct.Value));
		}

		public void SpecialMarkupUncheck()
		{
			if (CurrentSpecialMarkupProduct.Value == null)
				return;
			SpecialMarkupProducts.Value.Remove(CurrentSpecialMarkupProduct.Value);
		}

		private void SynchronizeSpecialMarkUps()
		{
			if (SpecialMarkupProducts.Value == null)
				return;
			var currentList = Session.Query<SpecialMarkupCatalog>().ToList();
			for (int i = 0; i < SpecialMarkupProducts.Value.Count; i++) {
				if (!currentList.Any(s => s.CatalogId == SpecialMarkupProducts.Value[i].CatalogId)) {
					Session.Save(SpecialMarkupProducts.Value[i]);
				}
			}
			for (int i = 0; i < currentList.Count; i++) {
				if (!SpecialMarkupProducts.Value.Any(s => s.CatalogId == currentList[i].CatalogId)) {
					var itemToDelete = Session.Query<SpecialMarkupCatalog>().FirstOrDefault(s => s.CatalogId == currentList[i].CatalogId);
					if (itemToDelete!=null) {
						Session.Delete(itemToDelete);
					}
				}
			}
		}

		public List<CustomStyle> Styles { get; set; }

		public Address CurrentAddress
		{
			get { return address; }
			set
			{
				address = value;
				CurrentWaybillSettings.Value = waybillConfig.FirstOrDefault(c => c.BelongsToAddress == value);
				PriceTagSettings.Value = Settings.Value.PriceTags.FirstOrDefault(r => r.Address == value);
			}
		}

		public NotifyValue<WaybillSettings> CurrentWaybillSettings { get; set; }
		public bool OverwritePriceTags { get; set; }
		public NotifyValue<PriceTagSettings> PriceTagSettings { get; set; }
		public NotifyValue<FrameworkElement> PriceTagPreview { get; set; }

		public void UpdatePriceTags()
		{
			if (!OverwritePriceTags)
				return;

			var dstAddresses = Addresses.Where(x => x != MarkupAddress.Value).ToArray();

			var items = Settings.Value.PriceTags.ToArray();
			var src = items.Where(x => x.Address == MarkupAddress.Value).ToArray();
			Settings.Value.PriceTags.RemoveEach(items.Except(src));
			Settings.Value.PriceTags.AddEach(dstAddresses.SelectMany(x => src.Select(y => new PriceTagSettings(y, x))));

			var priceTags = Session.Query<PriceTag>().ToArray();
			var priceTagSrc = priceTags.Where(r => r.AddressId == MarkupAddress.Value.Id).ToArray();
			var tagItems = Session.Query<PriceTagItem>().ToArray();
			var tagItemSrc = tagItems.Where(r => priceTagSrc.FirstOrDefault(x => x.Id == r.PriceTagId) != null).ToArray();
			Session.DeleteEach(tagItems.Except(tagItemSrc));
			Session.DeleteEach(priceTags.Except(priceTagSrc));
			Session.SaveEach(dstAddresses.SelectMany(a => priceTagSrc.Select(t => new PriceTag(t, a))));
			var newPriceTags = Session.Query<PriceTag>().Where(r => !priceTags.Contains(r)).ToArray();
			Session.SaveEach(newPriceTags.SelectMany(t => tagItemSrc.Select(i => new PriceTagItem(i, t))));
		}

		public NotifyValue<FrameworkElement> RackingMapPreview { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			MarkupAddress.Value = Address ?? Addresses.FirstOrDefault();
			CurrentAddress = Address ?? Addresses.FirstOrDefault();
			LoadPriceTagPreview();
			LoadRackingMapPreview();
		}

		private void LoadPriceTagPreview()
		{
			if (!MarkupAddress.HasValue)
				return;
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.PriceTag, MarkupAddress.Value))
				.Subscribe(x => PriceTagPreview.Value = x.Preview());
		}

		private void LoadRackingMapPreview()
		{
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.RackingMap, null))
				.Subscribe(x => RackingMapPreview.Value = x.Preview());
		}

		private static string Mask(string password1)
		{
			return new string(Enumerable.Repeat('*', (password1 ?? "").Length).ToArray());
		}

		public IList<MarkupConfig> MarkupByType(MarkupType type, Address currentddress)
		{
			var result = Settings.Value.Markups
				.Where(t => t.Type == type && t.Address == currentddress)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Value.Markups, i => Settings.Value.AddMarkup((MarkupConfig) i));
			MarkupConfig.Validate(result);
			return result;
		}

		public void NewVitallyImportantMarkup(InitializingNewItemEventArgs e)
		{
			var markup = (MarkupConfig) e.NewItem;
			markup.Type = MarkupType.VitallyImportant;
			markup.Address = MarkupAddress.Value;
		}

		public void NewNds18Markup(InitializingNewItemEventArgs e)
		{
			var markup = (MarkupConfig) e.NewItem;
			markup.Type = MarkupType.Nds18;
			markup.Address = MarkupAddress.Value;
		}

		public void NewSpecialMarkup(InitializingNewItemEventArgs e)
		{
			var markup = (MarkupConfig) e.NewItem;
			markup.Type = MarkupType.Special;
			markup.Address = MarkupAddress.Value;
		}

		public void NewMarkup(InitializingNewItemEventArgs e)
		{
			var markup = (MarkupConfig) e.NewItem;
			markup.Address = MarkupAddress.Value;
		}

		public IEnumerable<IResult> SelectWaybillDir()
		{
			var dir = Settings.Value.WaybillDir ?? Settings.Value.MapPath("Waybills");
			if (!Directory.Exists(dir))
				try
				{
					FileHelper.CreateDirectoryRecursive(dir);
				}
				catch { dir = Settings.Value.GetVarRoot(); }

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			Settings.Value.WaybillDir = dialog.Result;
			Settings.Value.IsDirectoryValid();
		}

		public IEnumerable<IResult> SelectRejectDir()
		{
			var dir = Settings.Value.RejectDir ?? Settings.Value.MapPath("Rejects");
			if (!Directory.Exists(dir))
				try
				{
					FileHelper.CreateDirectoryRecursive(dir);
				}
				catch { dir = Settings.Value.GetVarRoot(); }

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			Settings.Value.RejectDir = dialog.Result;
			Settings.Value.IsDirectoryValid();
		}

		public IEnumerable<IResult> SelectReportDir()
		{
			var dir = Settings.Value.ReportDir ?? Settings.Value.MapPath("Reports");
			if (!Directory.Exists(dir))
				try
				{
					FileHelper.CreateDirectoryRecursive(dir);
				}
				catch { dir = Settings.Value.GetVarRoot(); }

			var dialog = new SelectDirResult(dir);
			yield return dialog;
			Settings.Value.ReportDir = dialog.Result;
			Settings.Value.IsDirectoryValid();
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


		private void UpdateMarkupsByType(Address[] dstAddresses, MarkupType type)
		{
			var items = Settings.Value.Markups.Where(x => x.Type == type).ToArray();
			var src = items.Where(x => x.Address == MarkupAddress.Value).ToArray();
			Settings.Value.Markups.RemoveEach(items.Except(src));
			Settings.Value.Markups.AddEach(dstAddresses.SelectMany(x => src.Select(y => new MarkupConfig(y, x))));
		}

		public void UpdateMarkups()
		{
			var dstAddresses = Addresses.Where(x => x != MarkupAddress.Value).ToArray();
			if (OverwriteMarkups) {
				UpdateMarkupsByType(dstAddresses, MarkupType.Over);
			}
			if (OverwriteNds18Markups) {
				UpdateMarkupsByType(dstAddresses, MarkupType.Nds18);
			}
			if (OverwriteVitallyImportant) {
				UpdateMarkupsByType(dstAddresses, MarkupType.VitallyImportant);
			}
			if (OverwriteSpecialMarkups) {
				UpdateMarkupsByType(dstAddresses, MarkupType.Special);
			}
		}

		protected void GoToErrorTab(string errorKey)
		{
			switch (errorKey) {
				case "JunkPeriod":
					SelectedTab.Value = "ViewSettingsTab";
					break;
				default:
					SelectedTab.Value = errorKey + "MarkupsTab";
					break;
			}
		}

		public IEnumerable<IResult> Save()
		{
			UpdateMarkups();
			UpdatePriceTags();
			var error = Settings.Value.Validate(validateMarkups: HaveAddresses);

			if(error?.Count > 0){
				if (Session != null)
					Session.FlushMode = FlushMode.Never;
				GoToErrorTab(error.First()[0]);
				yield return MessageResult.Warn(error.First()[1]);
				yield break;
			}

			if (passwordUpdated)
				Settings.Value.Password = password;
			if (diadokPasswordUpdated)
				Settings.Value.DiadokPassword = diadokPassword;
			if (sbisPasswordUpdated)
				Settings.Value.SbisPassword = sbisPassword;

			if (App.Current != null)
				StyleHelper.BuildStyles(App.Current.Resources, Styles);

			if (Session != null) {
				IsCredentialsChanged = Session.IsChanged(Settings.Value, s => s.Password)
					|| Session.IsChanged(Settings.Value, s => s.UserName);
				if (Session.IsChanged(Settings.Value, s => s.GroupWaybillsBySupplier)
				    && Settings.Value.GroupWaybillsBySupplier) {
					foreach (var dirMap in DirMaps) {
						try {
							Directory.CreateDirectory(dirMap.Dir);
						}
						catch (Exception e) {
							Log.Error($"Не удалось создать директорию {dirMap.Dir}", e);
						}
					}
				}

				if (Session.IsChanged(Settings.Value, x => x.JunkPeriod))
					yield return new Models.Results.TaskResult(Query(s => DbMaintain.CalcJunk(s, Settings.Value)));

				Session.FlushMode = FlushMode.Auto;
				Settings.Value.ApplyChanges(Session);
				SynchronizeSpecialMarkUps();
			}
			TryClose();
		}


		public IEnumerable<IResult> ShowPriceTagConstructor()
		{
			yield return new DialogResult(new PriceTagConstructor(TagType.PriceTag, MarkupAddress.Value), fullScreen: true);
			LoadPriceTagPreview();
		}

		public IEnumerable<IResult> ShowRackingMapConstructor()
		{
			yield return new DialogResult(new PriceTagConstructor(TagType.RackingMap, null), fullScreen: true);
			LoadRackingMapPreview();
		}

		protected override void Broadcast()
		{
			Bus.SendMessage<Settings>(null);
		}

		public IEnumerable<IResult> EditColor(CustomStyle style)
		{
			return CustomStyle.Edit(style);
		}

		public IEnumerable<IResult> ScannerConfig()
		{
			var model = new ScannerConfig();
			yield return new DialogResult(model);
			Settings.Value.BarCodePrefix = model.Prefix;
			Settings.Value.BarCodeSufix = model.Sufix;
		}
	}
}