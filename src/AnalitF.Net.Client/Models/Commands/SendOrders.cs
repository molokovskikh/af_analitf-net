using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class SendOrders : RemoteCommand
	{
		private Address address;

		public SendOrders(Address address)
		{
			SuccessMessage = "Отправка заказов завершена успешно.";
			ErrorMessage = "Не удалось отправить заказы. Попробуйте повторить операцию позднее.";
			this.address = address;
		}

		protected override UpdateResult Execute()
		{
			Progress.OnNext(new Progress("Соединение", 100, 0));
			Progress.OnNext(new Progress("Отправка заказов", 0, 50));
			var orders =
				Session.Query<Order>().Where(o => o.Address == address && !o.Frozen && o.Send).ToList();
			if (orders.Count == 0)
				throw new EndUserError("Не заказов для отправки");

			var clientOrders = orders.Select(o => o.ToClientOrder()).Where(o => o != null).ToArray();

			var response =
				Client.PostAsync(new Uri(BaseUri, "Main").ToString(),
					new SyncRequest(clientOrders),
					Formatter,
					Token).Result;

			CheckResult(response);

			var sentOrders = orders.Select(o => new SentOrder(o)).ToArray();

			Session.SaveEach(sentOrders);
			foreach (var order in orders)
				Session.Delete(order);

			Progress.OnNext(new Progress("Отправка заказов", 100, 100));

			var settings = Session.Query<Settings>().First();
			if (settings.PrintOrdersAfterSend) {
				Results = new List<IResult> {
					new PrintResult("Отправленные заказы", sentOrders.Select(o => new OrderDocument(o)))
				};
			}

			return UpdateResult.OK;
		}
	}
}