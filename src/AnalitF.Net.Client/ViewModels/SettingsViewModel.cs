using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		public SettingsViewModel()
		{
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
		}

		public new Settings Settings { get; set; }

		public IList<MarkupConfig> Markups { get; set; }

		public IList<MarkupConfig> VitallyImportantMarkups { get; set; }

		public List<ValueDescription<DiffCalcMode>> DiffCalculationTypes { get; set; }

		public ValueDescription<DiffCalcMode> CurrentDiffCalculationType
		{
			get
			{
				return DiffCalculationTypes.First(t => t.Value ==  Settings.DiffCalcMode);
			}
			set
			{
				Settings.DiffCalcMode = value.Value;
			}
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