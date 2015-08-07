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

			if (existsJob == null) {
				existsJob  = new RequestLog(CurrentUser, Request, updateType, lastSync);
				existsJob.StartJob(Session, Config,
					(session, config, job) => {
						using (var exporter = new Exporter(session, config, job)) {
							if (data.Match("Waybills"))
								exporter.ExportDocs();
							else
								exporter.ExportAll();
							exporter.Compress(job.OutputFile(config));
						}
					});
			}

			return existsJob.ToResult(Config);
		}

		public HttpResponseMessage Delete()
		{
			var data = Session.Load<AnalitfNetData>(CurrentUser.Id);
			data.Confirm();
			Session.Save(data);

			Session.CreateSQLQuery(@"
update Usersettings.AnalitFReplicationInfo r
set r.ForceReplication = 0
where r.UserId = :userId and r.ForceReplication = 2;

update Logs.DocumentSendLogs l
	join Logs.PendingDocLogs p on p.SendLogId = l.Id
set l.Committed = 1
where p.UserId = :userId;

update Logs.MailSendLogs l
	join Logs.PendingMailLogs p on p.SendLogId = l.Id
set l.Committed = 1
where p.UserId = :userId;

delete from Logs.PendingDocLogs
where UserId = :userId;

update Orders.OrdersHead oh
	join Logs.PendingOrderLogs l on l.OrderId = oh.RowId
set oh.Deleted = 1
where l.UserId = :userId;

delete from Logs.PendingOrderLogs
where UserId = :userId;")
				.SetParameter("userId", CurrentUser.Id)
				.ExecuteUpdate();
			var job = Session.Query<RequestLog>().OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault(l => l.User == CurrentUser && l.IsCompleted && !l.IsConfirmed);
			if (job != null) {
				job.Confirm(Config);
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		public HttpResponseMessage Post(SyncRequest request)
		{
			SavePriceSettings(request.Prices);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		private void SavePriceSettings(PriceSettings[] settings)
		{
			if (settings == null)
				return;
			var log = "";
			foreach (var setting in settings) {
				var userPrice = Session.Query<UserPrice>().FirstOrDefault(u => u.User == CurrentUser
					&& u.Price.PriceCode == setting.PriceId
					&& u.RegionId == setting.RegionId);

				if (!setting.Active && userPrice != null) {
					log += String.Format("{0} {1} ({2}) {3} - выкл;", userPrice.Price.PriceCode,
						userPrice.Price.Supplier.Name,
						userPrice.Price.PriceName,
						userPrice.Price.Supplier.HomeRegion.Name);
					Session.Delete(userPrice);
				}
				else if (setting.Active && userPrice == null) {
					var price = Session.Get<PriceList>(setting.PriceId);
					if (price == null)
						return;
					userPrice = new UserPrice(CurrentUser, setting.RegionId, price);
					log += String.Format("{0} {1} ({2}) {3} - вкл;", userPrice.Price.PriceCode,
						userPrice.Price.Supplier.Name,
						userPrice.Price.PriceName,
						userPrice.Price.Supplier.HomeRegion.Name);
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
		}
	}
}
