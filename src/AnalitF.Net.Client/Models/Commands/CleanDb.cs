using System;
using System.Linq;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class CleanDb : DbCommand
	{
		public override void Execute()
		{
			var ignored = new [] { "SentOrders", "SentOrderLines", "Settings", "WaybillSettings", "MarkupConfigs"};
			var tables = Tables(Configuration).Except(ignored, StringComparer.InvariantCultureIgnoreCase).ToArray();

			using(var sesssion = Factory.OpenSession()) {
				var settings = sesssion.Query<Settings>().FirstOrDefault();
				if (settings != null)
					settings.LastUpdate = null;
				sesssion.Flush();

				foreach (var table in tables) {
					sesssion.CreateSQLQuery(String.Format("TRUNCATE {0}", table))
						.ExecuteUpdate();
				}
			}
		}
	}
}