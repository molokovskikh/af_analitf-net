using System;
using System.ComponentModel;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	[Description("Создает настройки наценок на сервере")]
	public class MarkupGlobalConfigSeriverFixture : ServerFixture
	{
		public void UpdateGlobalMarkups(ISession session, TestUser user, bool clientIsWithMarkupFlag)
		{
			session.CreateSQLQuery($"DELETE FROM usersettings.MarkupGlobalConfig WHERE ClientId = {user.Client.Id}")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"UPDATE customers.clients SET MarkupsSynchronization = {(clientIsWithMarkupFlag ? 1 : 0)} WHERE Id = {user.Client.Id}")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"INSERT INTO usersettings.MarkupGlobalConfig (ClientId, Type, Markup, MaxMarkup, MaxSupplierMarkup, Begin, End) VALUES ({user.Client.Id}, {1}, 10, 20, 20, 0, 50) ")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"INSERT INTO usersettings.MarkupGlobalConfig (ClientId, Type, Markup, MaxMarkup, MaxSupplierMarkup, Begin, End) VALUES ({user.Client.Id}, {1}, 10, 20, 20, 50, 500) ")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"INSERT INTO usersettings.MarkupGlobalConfig (ClientId, Type, Markup, MaxMarkup, MaxSupplierMarkup, Begin, End) VALUES ({user.Client.Id}, {1}, 10, 20, 20, 500, 1000000) ")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"INSERT INTO usersettings.MarkupGlobalConfig (ClientId, Type, Markup, MaxMarkup, MaxSupplierMarkup, Begin, End) VALUES ({user.Client.Id}, {0}, 10, 20, 20, 0, 1000000) ")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"INSERT INTO usersettings.MarkupGlobalConfig (ClientId, Type, Markup, MaxMarkup, MaxSupplierMarkup, Begin, End) VALUES ({user.Client.Id}, {2}, 10, 20, 20, 0, 1000000) ")
				.ExecuteUpdate();
			session.CreateSQLQuery(
				$"INSERT INTO usersettings.MarkupGlobalConfig (ClientId, Type, Markup, MaxMarkup, MaxSupplierMarkup, Begin, End) VALUES ({user.Client.Id}, {3}, 10, 20, 20, 0, 1000000) ")
				.ExecuteUpdate();
		}

		public override void Execute(ISession session)
		{
		}

		public override void Rollback(ISession session)
		{
		}
	}

	[Description("Создает настройки наценок на сервере")]
	public class MarkupGlobalConfigClientWithFlag : MarkupGlobalConfigSeriverFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			UpdateGlobalMarkups(session, user, true);
		}
	}

	[Description("Создает настройки наценок на сервере")]
	public class MarkupGlobalConfigClientWithoutFlag : MarkupGlobalConfigSeriverFixture
	{
		public override void Execute(ISession session)
		{
			var user = User(session);
			UpdateGlobalMarkups(session, user, false);
		}
	}
}