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

		protected ISession session;

		public BaseScreen()
		{
			session = Config.Initializers.NHibernate.Factory.OpenSession();
		}
	}
}