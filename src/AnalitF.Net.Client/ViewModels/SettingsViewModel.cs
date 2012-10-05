using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class SettingsViewModel : BaseScreen
	{
		public SettingsViewModel()
		{
			Settings = Session.Query<Settings>().First();
		}

		public Settings Settings { get; set; }

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