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
using log4net;
using log4net.Util.TypeConverters;
using NHibernate;

namespace AnalitF.Net.Service.Controllers
{
	public class DownloadController : ApiController
	{
		private ILog log = LogManager.GetLogger(typeof(DownloadController));

		public ISession Session { get; set; }
		public User CurrentUser { get; set; }
		public Config.Config Config;

		public HttpResponseMessage Post(string[] urns)
		{
			//временный файл будет удален после того как будет закрыт указатель на него
			var tmp = FileHelper.SelfDeleteTmpFile();
			using(var zip = ZipFile.Create(tmp)) {
				((ZipEntryFactory)zip.EntryFactory).IsUnicodeText = true;
				zip.BeginUpdate();
				foreach (var urn in urns) {
					log.DebugFormat("Загрузка {0}", urn);
					var parts = urn.Split(':').Skip(2).ToArray();
					if (parts.Length < 2) {
						log.WarnFormat("Недостаточно параметров {0}", urn);
						continue;
					}

					if (parts[0].Match("Attachment")) {
						var attachment = Session.Load<Attachment>(Convert.ToUInt32(parts[1]));
						var fileName = attachment.GetFilename(Config);
						if (!File.Exists(fileName)) {
							log.WarnFormat("Файл не найден {0}", fileName);
							continue;
						}
						zip.Add(fileName, urn.Replace(':', '.'));
						log.DebugFormat("Передан файл {0}", fileName);
					}
					else if (parts[0].Match("WaybillLine")) {
						var id = Convert.ToUInt32(parts[1]);
						var ids = Session.CreateSQLQuery(@"
select concat(cf.Id, cf.Extension)
from
	documents.DocumentBodies db
	inner join documents.DocumentHeaders dh on dh.Id = db.DocumentId
	inner join documents.SourceSuppliers ss on ss.SupplierId = dh.FirmCode
	inner join documents.Certificates c on c.Id = db.CertificateId
	inner join documents.FileCertificates fs on fs.CertificateId = c.Id
	inner join documents.CertificateFiles cf on cf.Id = fs.CertificateFileId and cf.CertificateSourceId = ss.CertificateSourceId
where
	db.Id = :bodyId")
							.SetParameter("bodyId", id)
							.List<string>();
						ids
							.Select(x => Path.Combine(Config.CertificatesPath, x))
							.Each(x => {
								if (File.Exists(x)) {
									log.DebugFormat("Передан файл {0}", x);
									zip.Add(x, Path.GetFileName(x));
								}
								else {
									log.WarnFormat("Не найден файл {0}", x);
								}
							});
					}
					else {
						log.WarnFormat("Неизвестный тип ресурса {0}", urn);
					}
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