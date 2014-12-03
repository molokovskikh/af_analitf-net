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
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Newtonsoft.Json;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class SendOrders : RemoteCommand
	{
		private Address address;

		public bool Force;

		public SendOrders(Address address, bool force = false)
		{
			SuccessMessage = "Отправка заказов завершена успешно.";
			ErrorMessage = "Не удалось отправить заказы. Попробуйте повторить операцию позднее.";
			this.address = address;
			this.Force = force;
		}

		protected override UpdateResult Execute()
		{
			var updateResult = UpdateResult.OK;
			Progress.OnNext(new Progress("Соединение", 100, 0));
			Progress.OnNext(new Progress("Отправка заказов", 0, 50));
			var orders = Session.Query<Order>().ReadyToSend(address).ToList();
			if (orders.Count == 0)
				throw new EndUserError("Не заказов для отправки");

			var clientOrders = orders.Select(o => o.ToClientOrder(Session)).Where(o => o != null).ToArray();
			log.InfoFormat("Попытка отправить заказы, всего заказов к отправке {0}", clientOrders.Length);

			var response =
				Client.PostAsync("Main",
					new SyncRequest(clientOrders, Force),
					Formatter,
					Token).Result;
			CheckResult(response);
			log.InfoFormat("Заказы отправлены успешно");

			var results = response.Content.ReadAsAsync<OrderResult[]>().Result
				?? new OrderResult[0];
			orders.Each(o => o.Apply(results.FirstOrDefault(r => r.ClientOrderId == o.Id)));
			var acceptedOrders = orders.Where(o => o.IsAccepted).ToArray();
			var rejectedOrders = orders.Where(o => !o.IsAccepted).ToArray();
			var sentOrders = acceptedOrders.Select(o => new SentOrder(o)).ToArray();

			Session.SaveEach(sentOrders);
			Session.DeleteEach(acceptedOrders);

			Progress.OnNext(new Progress("Отправка заказов", 100, 100));

			var settings = Session.Query<Settings>().First();
			if (rejectedOrders.Any()) {
				var user = Session.Query<User>().First();

				//если мы получили заказ без номера заказа с сервера значит он не принят
				//тк включена опция предзаказа и есть проблемы с другими заказами
				//если сервер уже знает что опция включена а клиент еще нет
				if (!user.IsPreprocessOrders)
					user.IsPreprocessOrders = rejectedOrders
						.SelectMany(r => r.Lines)
						.Any(l => l.SendResult != LineResultStatus.OK)
					|| rejectedOrders.Any(o => o.SendResult == OrderResultStatus.OK && o.ServerId == 0);

				if (!user.IsPreprocessOrders) {
					var resultText = rejectedOrders.Implode(
						o => String.Format("прайс-лист {0} - {1}", o.Price.Name, o.SendError),
						Environment.NewLine);
					var text = new TextViewModel(resultText) {
						Header = "Данные заказы НЕ ОТПРАВЛЕНЫ",
						DisplayName = "Не отправленные заказы"
					};
					Results.Add(new DialogResult(text, sizeToContent: true));
				}
				else {
					Results.Add(new DialogResult(new Correction(address.Id), fullScreen: true));
				}
				updateResult = UpdateResult.NotReload;
			}
			if (settings.PrintOrdersAfterSend && sentOrders.Length > 0) {
				Results.Add(new PrintResult("Отправленные заказы", sentOrders.Select(o => new OrderDocument(o))));
			}

			return updateResult;
		}
	}
}