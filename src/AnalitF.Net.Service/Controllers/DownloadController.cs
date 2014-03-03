using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using ICSharpCode.SharpZipLib.Zip;
using log4net.Util.TypeConverters;
using NHibernate;

namespace AnalitF.Net.Service.Controllers
{
	public class DownloadController : ApiController
	{
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }
		public Config.Config Config;

		public HttpResponseMessage Post(string[] urns)
		{
			//временный файл будет удален после того как будет закрыт указатель на него
			var tmp = FileHelper.DeleteOnCloseTmpFile();
			using(var zip = ZipFile.Create(tmp)) {
				((ZipEntryFactory)zip.EntryFactory).IsUnicodeText = true;
				zip.BeginUpdate();
				foreach (var urn in urns) {
					var parts = urn.Split(':').Skip(2).ToArray();
					if (parts.Length < 2)
						continue;
					var attachment = Session.Load<Attachment>(Convert.ToUInt32(parts[1]));
					var fileName = attachment.GetFilename(Config);
					if (!File.Exists(fileName))
						continue;
					zip.Add(fileName, urn.Replace(':', '.'));
				}
				zip.CommitUpdate();
			}

			tmp.Position = 0;
			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StreamContent(tmp)
			};
		}
	}
}