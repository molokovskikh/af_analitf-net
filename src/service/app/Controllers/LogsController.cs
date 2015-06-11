using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using log4net;
using NHibernate;
using NHibernate.Linq;

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
					IEnumerable<string> values;
					if (request.Headers.TryGetValues("Request-Token", out values)) {
						log.RequestToken = values.Implode();
					}

					if (String.IsNullOrWhiteSpace(log.Text))
						continue;

					Session.Save(log);
				}
			}
		}
	}
}