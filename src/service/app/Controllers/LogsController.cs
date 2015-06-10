using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
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

		public uint Post(HttpRequestMessage request)
		{
			var requestStream = request.Content.ReadAsStreamAsync().Result;

			var ids = new List<uint>();
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
					ids.Add(log.Id);
				}
			}
			return ids.FirstOrDefault();
		}

		public void Put(uint logId, uint requestId)
		{
			var log = Session.Query<ClientAppLog>().FirstOrDefault(x => x.User == CurrentUser && x.Id == logId);
			var request = Session.Query<RequestLog>().First(x => x.User == CurrentUser && x.Id == requestId);
			if (log == null || request == null)
				return;
			log.Request = request;
		}
	}
}