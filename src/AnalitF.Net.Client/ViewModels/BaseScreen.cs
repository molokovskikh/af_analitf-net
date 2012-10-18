using System;
using System.Linq;
using System.Linq.Expressions;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
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

		protected Settings Settings;

		public BaseScreen()
		{
			Session = Config.Initializers.NHibernate.Factory.OpenSession();
			Settings = Session.Query<Settings>().First();
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
		}
	}
}