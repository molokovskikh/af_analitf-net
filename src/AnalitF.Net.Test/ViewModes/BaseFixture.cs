using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.ViewModes
{
	public class BaseFixture
	{
		protected Client.Extentions.WindowManager manager;
		protected ShellViewModel shell;

		[SetUp]
		public void Setup()
		{
			shell = new ShellViewModel();
			manager = new Client.Extentions.WindowManager();
			manager.UnderTest = true;
			IoC.GetInstance = (type, key) => {
				return manager;
			};
		}

		protected T Init<T>(T model) where T : Screen
		{
			model.Parent = shell;
			return model;
		}
	}
}