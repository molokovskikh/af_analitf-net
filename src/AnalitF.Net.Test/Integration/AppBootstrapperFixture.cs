using System.IO;
using AnalitF.Net.Client;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Castle.Components.DictionaryAdapter;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture, RequiresSTA]
	public class AppBootstrapperFixture : BaseFixture
	{
		[Test]
		public void Persist_shell()
		{
			File.Delete("AnalitF.Net.Client.data");

			var app = CreateBootstrapper();
			app.InitShell();
			app.Shell.ViewSettings.Add("test", new EditableList<ColumnSettings> {
				new ColumnSettings()
			});
			app.Serialize();

			app = CreateBootstrapper();
			app.InitShell();
			Assert.That(app.Shell.ViewSettings["test"].Count, Is.EqualTo(1));
		}

		private AppBootstrapper CreateBootstrapper()
		{
			var app = new AppBootstrapper(false);
			Execute.ResetWithoutDispatcher();
			//setup - переопределяет windowmanager но AppBootstrapper вернет все назад
			//нужно восстановить тестовый windowmanager а то тесты начнут показывать окна
			StubWindowManager();
			return app;
		}
	}
}