﻿using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		public SettingsViewModel()
		{
			SelectedTab = new NotifyValue<string>("OverMarkupsTab");
			FlushOnClose = false;
			DisplayName = "Настройка";

			Settings = Session.Query<Settings>().First();

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

			if (string.IsNullOrEmpty(Settings.UserName)) {
				SelectedTab.Value = "LoginTab";
			}
		}

		public NotifyValue<string> SelectedTab { get; set; }

		public new Settings Settings { get; set; }

		public IList<MarkupConfig> Markups { get; set; }

		public IList<MarkupConfig> VitallyImportantMarkups { get; set; }

		public List<ValueDescription<DiffCalcMode>> DiffCalculationTypes { get; set; }

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