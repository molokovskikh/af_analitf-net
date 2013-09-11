using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
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
			var updateResult = UpdateResult.OK;
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

			var acceptedOrders = orders;
			var rejectedOrders = new List<Order>();

			var results = response.Content.ReadAsAsync<OrderResult[]>().Result;
			if (results != null) {
				foreach (var result in results) {
					var order = orders.FirstOrDefault(o => o.Id == result.ClientOrderId);
					if (order == null)
						continue;
					if (String.IsNullOrEmpty(result.Error)) {
						order.ServerId = result.ServerOrderId;
					}
					else {
						order.SendError = result.Error;
						acceptedOrders.Remove(order);
						rejectedOrders.Add(order);
					}
				}
			}
			var sentOrders = acceptedOrders.Select(o => new SentOrder(o)).ToArray();

			Session.SaveEach(sentOrders);
			Session.DeleteEach(acceptedOrders);

			Progress.OnNext(new Progress("Отправка заказов", 100, 100));

			var settings = Session.Query<Settings>().First();
			if (rejectedOrders.Count > 0) {
				var resultText = rejectedOrders.Implode(
					o => String.Format("прайс-лист {0} - {1}", o.Price.Name, o.SendError),
					Environment.NewLine);
				var text = new TextViewModel(resultText) {
					Header = "Данные заказы НЕ ОТПРАВЛЕНЫ",
					DisplayName = "Неотправленные заказы"
				};
				Results.Add(new DialogResult(text) { ShowFixed = true });
				updateResult = UpdateResult.Other;
			}
			if (settings.PrintOrdersAfterSend && sentOrders.Length > 0) {
				Results.Add(new PrintResult("Отправленные заказы", sentOrders.Select(o => new OrderDocument(o))));
			}

			return updateResult;
		}
	}
}