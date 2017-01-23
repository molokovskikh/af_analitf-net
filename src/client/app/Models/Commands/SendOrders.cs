using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Devart.Common;
using Newtonsoft.Json;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class SendOrders : RemoteCommand
	{
		private Address address;
		public bool Force;
		public IComparer<SentOrderLine> SortComparer;

		public SendOrders(Address address, bool force = false)
		{
			SuccessMessage = "Отправка заказов завершена успешно.";
			ErrorMessage = "Не удалось отправить заказы. Попробуйте повторить операцию позднее.";
			this.address = address;
			this.Force = force;
			RequestInterval = 2.Second();
		}

		protected override UpdateResult Execute()
		{
			var updateResult = UpdateResult.OK;
			Progress.OnNext(new Progress("Соединение", 100, 0));
			Client.BaseAddress = ConfigureHttp() ?? Client.BaseAddress;
			Progress.OnNext(new Progress("Отправка заказов", 0, 50));
			var orders = Session.Query<Order>().ReadyToSend(address).ToList();
			Log.InfoFormat("Попытка отправить заказы, всего заказов к отправке {0}", orders.Count);
			try {
				foreach (var order in orders) {
					Log.InfoFormat("Попытка отправки заказа {0} по прайсу {1} ({2}) от {3} с кол-вом позиций {4}",
						order.Id, order.PriceName, order.Price.Id.PriceId, order.Price.PriceDate, order.Lines.Count);
				}
			}
			catch(Exception e) {
				Log.Error("Ошибка протоколирования", e);
			}
			var clientOrders = orders.Select(o => o.ToClientOrder(Session)).Where(o => o != null).ToArray();
			if (clientOrders.Length == 0)
				throw new EndUserError("Не заказов для отправки");

			uint requestId = 0;
			var response = Wait("Orders",
				Client.PostAsync("Orders", new SyncRequest(clientOrders, Force), Formatter, Token), ref requestId);

			var results = response.Content.ReadAsAsync<OrderResult[]>().Result
				?? new OrderResult[0];
			CheckResult(Client.PutAsJsonAsync("Orders", new ConfirmRequest(requestId), Token));
			Log.InfoFormat("Заказы отправлены успешно");

			orders.Each(o => o.Apply(results.FirstOrDefault(r => r.ClientOrderId == o.Id)));
			var acceptedOrders = orders.Where(o => o.IsAccepted).ToArray();
			var rejectedOrders = orders.Where(o => !o.IsAccepted).ToArray();
			var sentOrders = acceptedOrders.Select(o => new SentOrder(o)).ToArray();
			acceptedOrders.Where(o => o.Limit != null).Each(o => o.Limit.Value -= o.Sum);
			foreach (var order in orders) {
				if (order.IsAccepted)
					Log.InfoFormat("Заказ {0} успешно отправлен, Id заказа на сервере: {1}", order.Id, order.ServerId);
				else
					Log.InfoFormat("Заказ {0} отвергнут сервером, причина: {1}", order.Id, order.SendError);
			}

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
						o => $"прайс-лист {o.Price.Name} - {o.SendError}",
						Environment.NewLine);
					var text = new TextViewModel(resultText) {
						Header = "Данные заказы НЕ ОТПРАВЛЕНЫ",
						DisplayName = "Не отправленные заказы"
					};
					Results.Add(new DialogResult(text));
				}
				else {
					Results.Add(new DialogResult(new Correction(address.Id), fullScreen: true));
				}
				updateResult = UpdateResult.NotReload;
			}
			if (sentOrders.Length > 0) {
				if (settings.PrintOrdersAfterSend) {
					Results.Add(new PrintResult("Отправленные заказы", sentOrders.Select(o => {
						var lines = o.Lines.ToList();
						if (SortComparer != null)
							lines.Sort(SortComparer);
						return new OrderDocument(o, lines.ToArray());
					})));
				}
				if (user.SaveOrders) {
					try {
						var dir = settings.InitAndMap("Orders");
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
						Log.Error("Ошибка при сохранении заявок", e);
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