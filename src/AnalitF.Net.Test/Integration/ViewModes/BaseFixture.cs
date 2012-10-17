using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class BaseFixture
	{
		protected Client.Extentions.WindowManager manager;
		protected ShellViewModel shell;
		protected ISession session;

		[SetUp]
		public void Setup()
		{
			session = Client.Config.Initializers.NHibernate.Factory.OpenSession();
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