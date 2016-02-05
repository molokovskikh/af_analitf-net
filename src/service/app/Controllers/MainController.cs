using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Service.Controllers
{
	public class MainController : JobController
	{
		public HttpResponseMessage Get(bool reset = false, string data = null, DateTime? lastSync = null)
		{
			var updateType = data ?? GetType().Name;
			var existsJob = TryFindJob(reset, updateType);
			//если есть не загруженные данные отдаем их
			if (reset && existsJob == null) {
					existsJob = Session.Query<RequestLog>()
					.Where(j => j.UpdateType == updateType && !j.IsConfirmed && !j.IsFaulted && j.User == CurrentUser)
					.OrderByDescending(j => j.CreatedOn)
					.Take(1)
					.ToArray()
					.FirstOrDefault(x => x.IsStale == false);
			}

			if (existsJob == null) {
				existsJob  = new RequestLog(CurrentUser, Request, updateType, lastSync);
				existsJob.StartJob(Session, Config,
					(session, config, job) => {
						using (var exporter = new Exporter(session, config, job)) {
							if (data.Match("Waybills"))
								exporter.ExportDocs();
							else
								exporter.ExportAll();
							//все данные выгружены завершаем транзакцию
							session.Transaction.Commit();
							exporter.Compress(job.OutputFile(config));
						}
					});
			}

			return existsJob.ToResult(Config);
		}

		public HttpResponseMessage Put(ConfirmRequest confirm)
		{
			var data = Session.Load<AnalitfNetData>(CurrentUser.Id);
			data.Confirm();
			Session.Save(data);

			//каждый запрос выполняется отдельно что бы проще было диагностировать блокировки
			Session.CreateSQLQuery(@"
update Usersettings.AnalitFReplicationInfo r
set r.ForceReplication = 0
where r.UserId = :userId and r.ForceReplication = 2;")
				.SetParameter("userId", CurrentUser.Id).ExecuteUpdate();

			Session.CreateSQLQuery(@"
update Logs.DocumentSendLogs l
	join Logs.PendingDocLogs p on p.SendLogId = l.Id
set l.Committed = 1, l.SendDate = now()
where p.UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id).ExecuteUpdate();

			Session.CreateSQLQuery(@"
delete from Logs.PendingDocLogs
where UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id).ExecuteUpdate();

			Session.CreateSQLQuery(@"
update Logs.MailSendLogs l
	join Logs.PendingMailLogs p on p.SendLogId = l.Id
set l.Committed = 1
where p.UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id).ExecuteUpdate();

			Session.CreateSQLQuery(@"
delete from Logs.PendingMailLogs
where UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id).ExecuteUpdate();

			Session.CreateSQLQuery(@"
update Orders.OrdersHead oh
	join Logs.PendingOrderLogs l on l.OrderId = oh.RowId
set oh.Deleted = 1
where l.UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id).ExecuteUpdate();

			Session.CreateSQLQuery(@"
delete from Logs.PendingOrderLogs
where UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id)
				.ExecuteUpdate();
			var job = Session.Get<RequestLog>((confirm?.RequestId).GetValueOrDefault())
				?? Session.Query<RequestLog>().OrderByDescending(j => j.CreatedOn)
					.FirstOrDefault(l => l.User == CurrentUser && l.IsCompleted && !l.IsConfirmed);
			job?.Confirm(Config, confirm?.Message);

			return new HttpResponseMessage(HttpStatusCode.OK);
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
