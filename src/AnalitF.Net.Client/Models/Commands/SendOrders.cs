using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using System.Linq;
using NHibernate.Linq;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class SendOrders : RemoteCommand
	{
		public Address Address;

		protected override UpdateResult Execute()
		{
			Progress.OnNext(new Progress("Соединение", 100, 0));
			Progress.OnNext(new Progress("Отправка заказов", 0, 50));
			var orders = Session.Query<Order>().Where(o => o.Address == Address && !o.Frozen && o.Send).ToList();
			if (orders.Count == 0)
				throw new EndUserError("Не заказов для отправки");

			var clientOrders = orders.Select(o => o.ToClientOrder()).Where(o => o != null).ToArray();

			var response = Client.PostAsync(new Uri(BaseUri, "Main").ToString(), new SyncRequest(clientOrders), Formatter, Token).Result;

			CheckResult(response);

			foreach (var order in orders)
				Session.Save(new SentOrder(order));

			foreach (var order in orders)
				Session.Delete(order);

			Progress.OnNext(new Progress("Отправка заказов", 100, 100));
			return UpdateResult.OK;
		}
	}
}