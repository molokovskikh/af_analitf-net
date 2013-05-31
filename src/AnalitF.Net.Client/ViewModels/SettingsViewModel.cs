using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		private Address address;
		private IList<WaybillSettings> waybillConfig;

		public SettingsViewModel()
		{
			SelectedTab = new NotifyValue<string>("OverMarkupsTab");
			CurrentWaybillSettings = new NotifyValue<WaybillSettings>();

			FlushOnClose = false;
			DisplayName = "Настройка";

			Settings = Session.Query<Settings>().First();
			waybillConfig = Session.Query<WaybillSettings>().ToList();
			Addresses = Session.Query<Address>().OrderBy(a => a.Name).ToList();
			CurrentAddress = Addresses.FirstOrDefault();

			var markups = Session.Query<MarkupConfig>().Where(t => t.Type == MarkupType.Over)
				.OrderBy(m => m.Begin)
				.ToList();
			MarkupConfig.Validate(markups.ToArray());
			Markups = new PersistentList<MarkupConfig>(markups, Session);

			var vitiallyImportant = Session.Query<MarkupConfig>().Where(t => t.Type == MarkupType.VitallyImportant)
				.OrderBy(m => m.Begin)
				.ToList();
			MarkupConfig.Validate(vitiallyImportant.ToArray());
			VitallyImportantMarkups = new PersistentList<MarkupConfig>(vitiallyImportant, Session);

			DiffCalculationTypes = Settings.DiffCalcMode.ToDescriptions<DiffCalcMode>();
			RackingMapSizes = Settings.RackingMap.Size.ToDescriptions<RackingMapSize>();
			PriceTagTypes = Settings.PriceTag.Type.ToDescriptions<PriceTagType>();
			CanConfigurePriceTag = new NotifyValue<bool>(() => CurrentPriceTagType.Value == PriceTagType.Normal);

			if (string.IsNullOrEmpty(Settings.UserName)) {
				SelectedTab.Value = "LoginTab";
			}
		}

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

		public void Save()
		{
			var isValid = MarkupConfig.Validate(Markups) && MarkupConfig.Validate(VitallyImportantMarkups);
			if (!isValid) {
				Manager.Warning("Некорректно введены границы цен.");
				return;
			}

			FlushOnClose = true;
			Settings.ApplyChanges(Session);
			TryClose();
		}
	}
}