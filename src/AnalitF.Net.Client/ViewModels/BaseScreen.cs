using System;
using System.Linq.Expressions;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class BaseScreen : Screen
	{
		protected ShellViewModel Shell
		{
			get { return ((ShellViewModel)Parent); }
		}

		protected Extentions.WindowManager Manager { get; private set; }

		protected ISession Session;

		public BaseScreen()
		{
			Session = Config.Initializers.NHibernate.Factory.OpenSession();
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
		}

	}
}