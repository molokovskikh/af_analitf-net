using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class DiadokSettings
	{
		public void Execute(ISession session)
		{
			Settings Settings = session.Query<Settings>().First();

			Settings.DiadokSignerJobTitle = "Должность";
			Settings.DiadokUsername = CreateDiadokInbox.ddkConfig.reciever_login;
			Settings.DiadokPassword = CreateDiadokInbox.ddkConfig.reciever_passwd;
			Settings.DebugDiadokSignerINN = CreateDiadokInbox.ddkConfig.reciever_inn;
			Settings.DebugUseTestSign = true;

			session.Save(Settings);
			session.Flush();
		}
	}
}