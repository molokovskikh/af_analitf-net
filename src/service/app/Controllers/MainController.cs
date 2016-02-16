﻿using System;
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

namespace AnalitF.Net.Service.Controllers
{
	public class MainController : JobController
	{
		public HttpResponseMessage Get(bool reset = false, string data = null, DateTime? lastSync = null)
		{
			var updateType = data ?? GetType().Name;
			var existsJob = TryFindJob(reset, updateType);
			if (Config.UpdateLifeTime > TimeSpan.Zero) {
				//если есть не загруженные данные отдаем их
				if (reset && existsJob == null) {
						existsJob = Session.Query<RequestLog>()
						.Where(j => j.UpdateType == updateType && !j.IsConfirmed && !j.IsFaulted && j.User == CurrentUser)
						.OrderByDescending(j => j.CreatedOn)
						.Take(1)
						.ToArray()
						.FirstOrDefault(x => x.GetIsStale(Config.UpdateLifeTime) == false);
				}
			}

			if (existsJob == null) {
				existsJob  = new RequestLog(CurrentUser, Request, updateType, lastSync);
				existsJob.StartJob(Session,
					(session, job) => {
						using (var exporter = new Exporter(session, Config, job)) {
							if (data.Match("Waybills"))
								exporter.ExportDocs();
							else
								exporter.ExportAll();
							//все данные выгружены завершаем транзакцию
							session.Transaction.Commit();
							exporter.Compress(job.OutputFile(Config));
						}
					});
			}

			return existsJob.ToResult(Config);
		}

		public HttpResponseMessage Put(ConfirmRequest request)
		{
			//при обновлении версии у нас нет идентификатора обновления
			var log = Session.Get<RequestLog>(request.RequestId)
				?? Session.Query<RequestLog>().OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault(l => l.User == CurrentUser && l.IsCompleted && !l.IsConfirmed);
			if (log == null)
				return new HttpResponseMessage(HttpStatusCode.OK);
			//если уже подтверждено значит мы получили информацию об импортированных заявках
			if (log.IsConfirmed) {
				log.Error += request.Message;
			} else {
				//записываем информацию о запросе что бы в случае ошибки повторить попытку
				var failsafe = Path.Combine(Config.FailsafePath, log.Id.ToString());
				File.WriteAllText(failsafe, JsonConvert.SerializeObject(request));
				Task = RequestLog.RunTask(Session, x => Confirm(x, CurrentUser.Id, request, Config));
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		public static void Confirm(ISession session, uint userId, ConfirmRequest request, Config.Config config)
		{
			var failsafe = Path.Combine(config.FailsafePath, request.RequestId.ToString());
			try {
				Exporter.Confirm(session, userId, request, config);
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
