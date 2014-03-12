using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AnalitF.Net.Service.Helpers;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using Newtonsoft.Json;
using NHibernate;
using SmartOrderFactory;
using SmartOrderFactory.Domain;

namespace AnalitF.Net.Service.Controllers
{
	public class BatchController : ApiController
	{
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }
		public Config.Config Config;

		public HttpResponseMessage Get()
		{
			return new HttpResponseMessage(HttpStatusCode.NotImplemented);
		}

		public HttpResponseMessage Post(HttpRequestMessage request)
		{
			var job = new RequestLog(CurrentUser, Request);
			job.UpdateType = "SmartOrder";
			Session.Save(job);

			try
			{
				var input = request.Content.ReadAsStreamAsync().Result;
				List<Order> orders;
				List<OrderBatchItem> batchItems;
				Address batchAddress;
				using(var zip = ZipFile.Read(input)) {
					var meta = zip.Entries.First(e => e.FileName.Match("meta.json"));
					var serializer = new JsonSerializer();
					BatchRequest requestMeta;
					using(var streamReader = new StreamReader(meta.OpenReader())) {
						requestMeta = (BatchRequest)serializer.Deserialize(streamReader, typeof(BatchRequest));
					}
					var payload = zip.Entries.First(e => e.FileName.Match("payload"));

					batchAddress = Session.Load<Address>(requestMeta.AddressId);
					using (var stream = payload.OpenReader()) {
						var handler = new SmartOrderBatchHandler(CurrentUser, batchAddress, stream);
						orders = handler.ProcessOrderBatch();
						batchItems = handler.OrderBatchItems;
						orders.Each(o => o.RowId = (uint)o.GetHashCode());
						orders.SelectMany(o => o.OrderItems).Each(o => o.RowId = (uint)o.GetHashCode());
					}
				}
				using(var exporter = new Exporter(Session, Config, job)) {
					exporter.Orders = orders;
					exporter.BatchItems = batchItems;
					exporter.BatchAddress = batchAddress;
					exporter.ExportCompressed(job.OutputFile(Config));
					return new HttpResponseMessage(HttpStatusCode.OK) {
						Content = new StreamContent(job.GetResult(Config))
					};
				}
			}
			catch(Exception e) {
				job.Faulted(e);
				throw;
			}
			finally {
				job.Completed();
			}
		}
	}
}