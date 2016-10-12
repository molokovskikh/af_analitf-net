using System.Net.Http;
using AnalitF.Net.Service.Models;

namespace AnalitF.Net.Service.Controllers
{
	public class HistoryController : JobController2
	{
		public HttpResponseMessage Post(HistoryRequest request)
		{
			return StartJob((session, config, job) => {
				using (var exporter = new Exporter(session, config, job)) {
					exporter.ExportSentOrders(request.OrderIds ?? new ulong[0]);
					exporter.Compress(job.OutputFile(Config));
				}
			});
		}
	}
}