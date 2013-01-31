using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using NHibernate.Collection.Generic;
using NHibernate.Linq;
using NHibernate.Persister.Collection;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		public SettingsViewModel()
		{
			Settings = Session.Query<Settings>().First();
			var markups = Session.Query<MarkupConfig>().OrderBy(m => m.Begin).ToList();
			Markups = new PersistentList<MarkupConfig>(markups, Session);

			DiffCalculationTypes = Settings.DiffCalcMode.ToDescriptions<DiffCalcMode>();
			DisplayName = "Настройка";
		}

		public new Settings Settings { get; set; }

		public IList<MarkupConfig> Markups { get; set; }

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
			Settings.ApplyChanges(Session);
			Session.Save(Settings);
			TryClose();
		}
	}
}