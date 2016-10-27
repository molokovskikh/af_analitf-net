using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;

namespace AnalitF.Net.Service.Controllers
{
	public class MainController : JobController
	{
		public HttpResponseMessage Get(bool reset = false,
			string data = null,
			DateTime? lastSync = null,
			//перечень активных адресов доставки через запятую
			//для экспорта неподтвержденных заявок
			string addressIds = null,
			uint? id = null)
		{
			if (id != null)
				return Session.Load<RequestLog>(id.Value).ToResult(Request, Config);

			var updateType = data ?? GetType().Name;
			RequestLog existsJob = null;
			//если это новый запрос то пытаемся найти уже подготовленные данные которые не протухли или в процессе
			//что бы избежать дополнительной нагрузки на сервер
			if (reset) {
				if (Config.UpdateLifeTime > TimeSpan.Zero) {
					var version = RequestLog.GetVersion(Request);
					//если есть не загруженные данные отдаем их
					//каждая версия хранит подготовленные данные в своей директории
					//по этому передавать неподтвержденные данные есть смысл только в рамках версии
					existsJob = Session.Query<RequestLog>()
						.Where(j => j.UpdateType == updateType
							&& !j.IsConfirmed
							&& !j.IsFaulted
							&& j.User == CurrentUser
							&& j.Version == version)
						.OrderByDescending(j => j.CreatedOn)
						.Take(1)
						.ToArray()
						.FirstOrDefault(x => x.GetIsStale(Config.UpdateLifeTime) == false);
				}
			} else {
				existsJob = TryFindJob(updateType);
			}

			if (existsJob == null) {
				existsJob  = new RequestLog(CurrentUser, Request, updateType, lastSync);
				existsJob.StartJob(Session,
					(session, job) => {
						using (var exporter = new Exporter(session, Config, job)) {
							exporter.Addresses = addressIds?.Split(',')
								.Select(x => session.Load<Address>(Convert.ToUInt32(x)))
								.ToArray()
								?? new Address[0];
							if (data.Match("Waybills")) {
								exporter.ExportDocs();
							} else {
								//если есть обновление исполняемых файлов то готовить данные нет смысла
								//тк схема данных могла измениться и клиент все равно будет загружать кумулятивное
								//после обновления бинарных файлов
								if (data.Match("NoBin") || !exporter.ExportBin())
									exporter.ExportDb();
							}
							//все данные выгружены завершаем транзакцию
							session.Transaction.Commit();
							exporter.Compress(job.OutputFile(Config));
						}
					});
			}

			return existsJob.ToResult(Request, Config);
		}

		public HttpResponseMessage Put(ConfirmRequest request)
		{
#if DEBUG
			if (request.RequestId == 0)
				throw new Exception("При подтверждении должен быть указан идентификатор обновления");
#endif
			//из-за изменения схемы подтверждения обновления при переходе с версии на версию идентификатор обновления
			//не передается
			if (request.RequestId == 0)
				return new HttpResponseMessage(HttpStatusCode.OK);
			var log = Session.Get<RequestLog>(request.RequestId);
			//если уже подтверждено значит мы получили информацию об импортированных заявках
			if (log.IsConfirmed) {
				log.Error += request.Message;
				var userId = CurrentUser.Id;
				var messageShowCountList = Session.CreateSQLQuery(@"
select MessageShowCount from usersettings.userupdateinfo where UserID = :userId")
				.SetParameter("userId", userId).List();

			var messageShowCount = Convert.ToByte(messageShowCountList.First());
			if (messageShowCount > 0)
				messageShowCount--;

			Session.CreateSQLQuery(@"
update usersettings.userupdateinfo
set MessageShowCount = :MessageShowCount
where UserId = :userId;")
				.SetParameter("MessageShowCount", messageShowCount)
				.SetParameter("userId", userId)
				.ExecuteUpdate();
			} else {
				//записываем информацию о запросе что бы в случае ошибки повторить попытку
				var failsafe = Path.Combine(Config.FailsafePath, log.Id.ToString());
				File.WriteAllText(failsafe, JsonConvert.SerializeObject(request));
				var task = RequestLog.RunTask(Session, x => Confirm(x, log.Id, Config, request));
				if (task.IsFaulted)
					return new HttpResponseMessage(HttpStatusCode.InternalServerError);
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		public static void Confirm(ISession session, uint requestLogId, Config.Config config, ConfirmRequest request)
		{
			var failsafe = Path.Combine(config.FailsafePath, requestLogId.ToString());
			try {
				new Exporter(session, config, session.Load<RequestLog>(requestLogId)).Confirm(request);
				File.Delete(failsafe);
			}
			catch(Exception) {
				File.Move(failsafe, Path.ChangeExtension(failsafe, ".err"));
				throw;
			}
		}

		public HttpResponseMessage Post(SyncRequest request)
		{
			if (request.Prices == null)
				return new HttpResponseMessage(HttpStatusCode.OK);

			var log = "";
			foreach (var setting in request.Prices) {
				var userPrice = Session.Query<UserPrice>().FirstOrDefault(u => u.User == CurrentUser
					&& u.Price.PriceCode == setting.PriceId
					&& u.RegionId == setting.RegionId);

				if (!setting.Active && userPrice != null) {
					log += $"{userPrice.Price.PriceCode} {userPrice.Price.Supplier.Name}" +
						$" ({userPrice.Price.PriceName}) {userPrice.Price.Supplier.HomeRegion.Name} - выкл;";
					Session.Delete(userPrice);
				}
				else if (setting.Active && userPrice == null) {
					var price = Session.Get<PriceList>(setting.PriceId);
					if (price == null)
						continue;
					userPrice = new UserPrice(CurrentUser, setting.RegionId, price);
					log += $"{userPrice.Price.PriceCode} {userPrice.Price.Supplier.Name}" +
						$" ({userPrice.Price.PriceName}) {userPrice.Price.Supplier.HomeRegion.Name} - вкл;";
					Session.Save(userPrice);
				}
			}
			if (!String.IsNullOrEmpty(log)) {
				Session.Save(new RequestLog(CurrentUser, Request, "Prices") {
					Error = log,
					IsConfirmed = true,
					IsCompleted = true,
				});
			}
			return new HttpResponseMessage(HttpStatusCode.OK);
		}
	}
}
