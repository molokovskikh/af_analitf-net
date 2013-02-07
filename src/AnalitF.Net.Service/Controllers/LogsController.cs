using System;
using System.IO;
using System.Net.Http;
using System.Web.Http;
using AnalitF.Net.Models;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using NHibernate;
using log4net;

namespace AnalitF.Net.Controllers
{
	public class LogsController : ApiController
	{
		private FileCleaner cleaner = new FileCleaner();

		public static ILog log = LogManager.GetLogger(typeof(LogsController));

		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public void Post(HttpRequestMessage request)
		{
			var requestStream = request.Content.ReadAsStreamAsync().Result;
			var file = Path.GetTempFileName();
			cleaner.Watch(file);
			using (var stream = File.OpenWrite(file)) {
				requestStream.CopyTo(stream);
			}
			using(var zip = new ZipFile(file)) {
				foreach (var entry in zip) {
					var memory = new MemoryStream();
					entry.Extract(memory);
					memory.Position = 0;
					Session.Save(new ClientAppLog(CurrentUser, new StreamReader(memory).ReadToEnd()));
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			cleaner.Dispose();
		}
	}
}