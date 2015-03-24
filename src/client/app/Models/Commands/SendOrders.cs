using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
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
using Devart.Common;
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

			uint requestId = 0;
			var response = Wait("Orders",
				Client.PostAsync("Orders", new SyncRequest(clientOrders, Force), Formatter, Token),
				ref requestId);
			CheckResult(Client.PutAsJsonAsync("Orders", new ConfirmRequest(requestId), Token).Result);

			log.InfoFormat("Заказы отправлены успешно");

			var results = response.Content.ReadAsAsync<OrderResult[]>().Result
				?? new OrderResult[0];
			orders.Each(o => o.Apply(results.FirstOrDefault(r => r.ClientOrderId == o.Id)));
			var acceptedOrders = orders.Where(o => o.IsAccepted).ToArray();
			var rejectedOrders = orders.Where(o => !o.IsAccepted).ToArray();
			var sentOrders = acceptedOrders.Select(o => new SentOrder(o)).ToArray();
			acceptedOrders.Where(o => o.Limit != null).Each(o => o.Limit.Value -= o.Sum);

			Session.SaveEach(sentOrders);
			Session.DeleteEach(acceptedOrders);

			Progress.OnNext(new Progress("Отправка заказов", 100, 100));

			var settings = Session.Query<Settings>().First();
			var user = Session.Query<User>().First();
			if (rejectedOrders.Any()) {
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
			if (sentOrders.Length > 0) {
				if (settings.PrintOrdersAfterSend) {
					Results.Add(new PrintResult("Отправленные заказы", sentOrders.Select(o => new OrderDocument(o))));
				}
				if (user.SaveOrders) {
					try {
						var dir = settings.MapPath("Orders");
						if (!Directory.Exists(dir))
							Directory.CreateDirectory(dir);

						foreach (var sentOrder in sentOrders) {
							var name = Path.Combine(dir, sentOrder.ServerId + ".txt");
							using(var writer = new StreamWriter(name, false, Encoding.Default)) {
								writer.WriteLine("Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество;Приоритет;Цена;Поставщик");
								foreach (var line in sentOrder.Lines) {
									var payload = new string[0];
									var orderLine = orders.SelectMany(o => o.Lines).FirstOrDefault(l => l.ExportId == line.ServerId);
									if (orderLine != null && orderLine.ExportBatchLineId != null) {
										var batchline = StatelessSession.Query<BatchLine>()
											.FirstOrDefault(b => b.ExportId == orderLine.ExportBatchLineId.Value);
										if (batchline != null)
											payload = (batchline.ParsedServiceFields.GetValueOrDefault("ReportData") ?? "").Split(';');
									}

									writer.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}",
										GetIndexOrDefault(payload, 0),
										GetIndexOrDefault(payload, 1),
										sentOrder.SentOn,
										GetIndexOrDefault(payload, 3),
										line.ProductSynonym,
										GetIndexOrDefault(payload, 5),
										line.ProducerSynonym,
										line.Count,
										GetIndexOrDefault(payload, 9),
										GetIndexOrDefault(payload, 10),
										line.Order.Price.Name);
								}
							}
						}
					}
					catch(Exception e) {
#if DEBUG
						throw;
#else
						log.Error("Ошибка при сохранении заявок", e);
#endif
					}
				}
			}

			return updateResult;
		}

		public static string GetIndexOrDefault(string[] array, int index)
		{
			if (index >= array.Length)
				return "";
			return array[index];
		}
	}
}