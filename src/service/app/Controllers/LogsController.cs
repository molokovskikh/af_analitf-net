using System;
using System.IO;
using System.Net.Http;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using Ionic.Zip;
using log4net;
using NHibernate;

namespace AnalitF.Net.Service.Controllers
{
	public class LogsController : ApiController
	{
		public static ILog log = LogManager.GetLogger(typeof(LogsController));

		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public void Post(HttpRequestMessage request)
		{
			var requestStream = request.Content.ReadAsStreamAsync().Result;

			using(var zip = ZipFile.Read(requestStream)) {
				foreach (var entry in zip) {
					var memory = new MemoryStream();
					entry.Extract(memory);
					memory.Position = 0;
					var log = new ClientAppLog(CurrentUser, new StreamReader(memory).ReadToEnd());
					log.Version = RequestLog.GetVersion(Request);
					if (String.IsNullOrWhiteSpace(log.Text))
						continue;

					Session.Save(log);
				}
			}
		}
	}
}