using System.Collections.Generic;
using System.ComponentModel;
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
			Settings = Session.Query<Settings>().First();
			DiffCalculationTypes = Settings.DiffCalcMode.ToDescriptions<DiffCalcMode>();
			DisplayName = "Настройка";
		}

		public Settings Settings { get; set; }

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

		protected override void OnDeactivate(bool close)
		{
			Session.Flush();
		}
	}
}