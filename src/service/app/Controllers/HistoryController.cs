using System;
using System.Net;
using System.Net.Http;
using System.Text;
using AnalitF.Net.Service.Helpers;
using AnalitF.Net.Service.Models;
using NHibernate;

namespace AnalitF.Net.Service.Controllers
{
	public class HistoryController : JobController2
	{
		public HttpResponseMessage Post(HistoryRequest request)
		{
			return StartJob((session, config, job) => {
				using (var exporter = new Exporter(session, config, job)) {
					if (!request.IgnoreOrders) {
						exporter.ExportSentOrders(request.OrderIds ?? new ulong[0]);
					}

					if (!request.IgnoreWaybills) {
						var condition = new StringBuilder();
						if (request.WaybillIds.Length > 0) {
							condition.Append(" and ds.DocumentId not in (");
							condition.Append(String.Join(", ", request.WaybillIds));
							condition.Append(") ");
						}

						session.CreateSQLQuery("update Logs.DocumentSendLogs ds " +
							" set ds.Committed = 0, ds.FileDelivered = 0, ds.DocumentDelivered = 0 " +
							" where ds.UserId = :userId " +
							condition)
							.SetParameter("userId", job.User.Id)
							.ExecuteUpdate();
						exporter.ExportDocs();
					}
					exporter.Compress(job.OutputFile(Config));
				}
			});
		}
	}
}