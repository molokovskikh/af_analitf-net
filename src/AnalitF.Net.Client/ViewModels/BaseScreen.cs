using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
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
		protected IStatelessSession StatelessSession;

		protected Settings Settings;

		public static IScheduler Scheduler = DefaultScheduler.Instance;

		public BaseScreen()
		{
			var factory = AppBootstrapper.NHibernate.Factory;
			StatelessSession = factory.OpenStatelessSession();
			Session = factory.OpenSession();
			Settings = Session.Query<Settings>().First();
			Manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
		}
	}
}