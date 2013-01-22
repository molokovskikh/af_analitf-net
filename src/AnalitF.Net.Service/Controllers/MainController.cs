using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using AnalitF.Net.Models;
using Common.Models;
using Common.MySql;
using Common.Tools;
using Ionic.Zip;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using log4net;

namespace AnalitF.Net.Controllers
{
	public class MainController : ApiController
	{
		public static ILog log = LogManager.GetLogger(typeof(MainController));

		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public HttpResponseMessage Get(bool reset = false)
		{
			var existsJob = Session.Query<RequestLog>()
				.OrderByDescending(j => j.CreatedOn)
				.FirstOrDefault(j => j.User == CurrentUser);

			if (existsJob != null) {
				if (existsJob.IsStale)
					existsJob = null;
				else if (reset)
					existsJob = null;
			}

			if (existsJob == null) {
				var version = new Version();
				IEnumerable<string> headers;
				if (Request.Headers.TryGetValues("Version", out headers)) {
					Version.TryParse(headers.FirstOrDefault(), out version);
				}

				existsJob  = new RequestLog(CurrentUser, version);
				Session.Save(existsJob);
				Session.Transaction.Commit();

				StartJob(existsJob.Id, Session.SessionFactory);
			}

			if (!existsJob.IsCompleted)
				return new HttpResponseMessage(HttpStatusCode.Accepted);

			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StreamContent(existsJob.GetResult(FileHelper.MakeRooted(ConfigurationManager.AppSettings["ResultPath"])))
			};
		}

		public static Task StartJob(uint jobId, ISessionFactory sessionFactory)
		{
			var task = new Task(() => {
				try {
					using (var logSession = sessionFactory.OpenSession())
					using (var logTransaction = logSession.BeginTransaction()) {
						var job = logSession.Load<RequestLog>(jobId);
						try {
							using(var exportSession = sessionFactory.OpenSession())
							using(var exportTransaction = exportSession.BeginTransaction()) {
								var exporter = new Exporter(exportSession, job.User.Id, job.Version) {
									Prefix = job.Id.ToString(),
									ExportPath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["ExportPath"]),
									ResultPath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["ResultPath"]),
									UpdatePath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["UpdatePath"]),
									AdsPath = FileHelper.MakeRooted(ConfigurationManager.AppSettings["AdsPath"]),
									MaxProducerCostPriceId = SafeConvert.ToUInt32(ConfigurationManager.AppSettings["MaxProducerCostPriceId"]),
									MaxProducerCostCostId = SafeConvert.ToUInt32(ConfigurationManager.AppSettings["MaxProducerCostCostId"]),
								};
								using (exporter) {
									exporter.ExportCompressed(job.OutputFile);
								}
								exportTransaction.Commit();
							}
						}
						catch(Exception e) {
							log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
							job.IsFaulted = true;
							job.Error = e.ToString();
						}
						finally {
							job.IsCompleted = true;
							logSession.Save(job);
							logSession.Flush();
							logTransaction.Commit();
						}
					}
				}
				catch(Exception e) {
					log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
				}
			});
			task.Start();
			return task;
		}

		public HttpResponseMessage Post(ClientOrder[] clientOrders)
		{
			StorageProcedures.GetActivePrices((MySqlConnection)Session.Connection, CurrentUser.Id);

			var rules = Session.Load<OrderRules>(CurrentUser.Client.Id);
			foreach (var clientOrder in clientOrders) {
				var address = Session.Load<Address>(clientOrder.AddressId);
				var price = Session.Load<PriceList>(clientOrder.PriceId);
				var activePrice = Session.Load<ActivePrice>(new PriceKey(price, clientOrder.RegionId));

				var order = new Order(activePrice, CurrentUser, address, rules) {
					ClientOrderId = clientOrder.ClientOrderId,
					PriceDate = clientOrder.PriceDate,
					ClientAddition = clientOrder.Comment,
				};
				foreach (var item in clientOrder.Items) {
					var offer = new Offer {
						Id = new OfferKey(item.OfferId.OfferId, item.OfferId.RegionId),
						Cost = (float)item.Cost,
						PriceList = activePrice,
						PriceCode = price.PriceCode,
						CodeFirmCr = item.ProducerId,
					};

					var properties = typeof(BaseOffer).GetProperties().Where(p => p.CanRead && p.CanWrite);
					foreach (var property in properties) {
						var value = property.GetValue(item, null);
						property.SetValue(offer, value, null);
					}

					order.AddOrderItem(offer, item.Count);
				}
				Session.Save(order);
			}
			return new HttpResponseMessage(HttpStatusCode.OK);
		}
	}
}
