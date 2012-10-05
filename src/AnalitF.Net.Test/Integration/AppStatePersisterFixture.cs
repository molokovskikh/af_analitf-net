using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class AppStatePersisterFixture : BaseFixture
	{
		[Test]
		public void Save()
		{
			var model = new ShellViewModel();
			model.ActiveItem = new MnnViewModel {
				SearchText = "папа"
			};
			AppBootstrapper.Shell = model;
			Persister.SaveState();
		}
	}
}