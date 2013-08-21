using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.NHibernate;
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
			DirMaps = Session.Query<DirMap>().OrderBy(d => d.Supplier.FullName).ToList();
			CurrentDirMap.Value = DirMaps.FirstOrDefault();

			Markups = Settings.Markups.Where(t => t.Type == MarkupType.Over)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Markups);
			MarkupConfig.Validate(Markups);

			VitallyImportantMarkups = Settings.Markups
				.Where(t => t.Type == MarkupType.VitallyImportant)
				.OrderBy(m => m.Begin)
				.LinkTo(Settings.Markups);
			MarkupConfig.Validate(VitallyImportantMarkups);

			DiffCalculationTypes = Settings.DiffCalcMode.ToDescriptions<DiffCalcMode>();
			RackingMapSizes = Settings.RackingMap.Size.ToDescriptions<RackingMapSize>();
			PriceTagTypes = Settings.PriceTag.Type.ToDescriptions<PriceTagType>();
			Taxations = DescriptionHelper.GetDescription<Taxation>();
			CanConfigurePriceTag = new NotifyValue<bool>(() => CurrentPriceTagType.Value == PriceTagType.Normal);
			CurrentWaybillSettings.Changed().Subscribe(_ => NotifyOfPropertyChange("CurrentTaxation"));

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

		public List<ValueDescription<DiffCalcMode>> DiffCalculationTypes { get; set; }

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

		public ValueDescription<DiffCalcMode> CurrentDiffCalculationType
		{
			get { return DiffCalculationTypes.First(t => t.Value ==  Settings.DiffCalcMode); }
			set { Settings.DiffCalcMode = value.Value; }
		}

		public List<ValueDescription<RackingMapSize>> RackingMapSizes { get; set; }

		public ValueDescription<RackingMapSize> CurrentRackingMapSize
		{
			get { return RackingMapSizes.First(x => x.Value == Settings.RackingMap.Size); }
			set { Settings.RackingMap.Size = value.Value; }
		}

		public List<ValueDescription<PriceTagType>> PriceTagTypes { get; set; }

		public ValueDescription<PriceTagType> CurrentPriceTagType
		{
			get { return PriceTagTypes.FirstOrDefault(t => t.Value == Settings.PriceTag.Type); }
			set
			{
				Settings.PriceTag.Type = value.Value;
				CanConfigurePriceTag.Recalculate();
			}
		}

		public NotifyValue<bool> CanConfigurePriceTag { get; set; }

		public void NewVitallyImportantMarkup(InitializingNewItemEventArgs e)
		{
			((MarkupConfig)e.NewItem).Type = MarkupType.VitallyImportant;
		}

		public List<ValueDescription<Taxation>> Taxations { get; set; }

		public ValueDescription<Taxation> CurrentTaxation
		{
			get
			{
				return CurrentWaybillSettings.Value == null
					? null
					: Taxations.First(t => t.Value == CurrentWaybillSettings.Value.Taxation) ;
			}
			set
			{
				if (CurrentWaybillSettings.Value == null || value == null)
					return;
				CurrentWaybillSettings.Value.Taxation = value.Value;
			}
		}

		public IEnumerable<IResult> SelectDir()
		{
			if (CurrentDirMap.Value == null)
				yield break;

			var dialog = new SelectDirResult(CurrentDirMap.Value.Dir);
			yield return dialog;
			CurrentDirMap.Value.Dir = dialog.Result;
		}

		public void Save()
		{
			var result1 = MarkupConfig.Validate(VitallyImportantMarkups);
			var result2 = MarkupConfig.Validate(Markups);
			var total = Tuple.Create(result1.Item1 && result2.Item1, result1.Item2 ?? result2.Item2);
			var isValid = total.Item1;

			if (!isValid) {
				Session.FlushMode = FlushMode.Never;
				Manager.Warning(total.Item2 ?? "Некорректно введены границы цен.");
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