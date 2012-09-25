using Caliburn.Micro;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels
{
	public class BaseScreen : Screen
	{
		protected ShellViewModel Shell
		{
			get { return ((ShellViewModel)Parent); }
		}

		protected IWindowManager Manager { get; private set; }

		protected ISession Session;

		public BaseScreen()
		{
			Session = Config.Initializers.NHibernate.Factory.OpenSession();
			Manager = IoC.Get<IWindowManager>();
		}
	}
}