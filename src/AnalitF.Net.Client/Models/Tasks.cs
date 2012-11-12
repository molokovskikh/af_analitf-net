using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;

namespace AnalitF.Net.Client.Models
{
	public class RequestException : Exception
	{
		public RequestException(string message, HttpStatusCode statusCode) : base(message)
		{
			StatusCode = statusCode;
		}

		public HttpStatusCode StatusCode { get; set; }
	}

	public class Tasks
	{
		public static Func<ICredentials, CancellationToken, Task> Update = (c, t) => UpdateTask(c, t);

		public static Uri Uri;
		public static string ArchiveFile;
		public static string ExtractPath;

		public static Task UpdateTask(ICredentials credentials, CancellationToken cancellation)
		{
			return new Task(() => {
				var handler = new HttpClientHandler {
					Credentials = credentials,
					PreAuthenticate = true
				};
				using (handler) {
					using (var client = new HttpClient(handler)) {
						var done = false;
						HttpResponseMessage response = null;

						while (!done) {
							var request = client.GetAsync(Uri, HttpCompletionOption.ResponseHeadersRead, cancellation);
							response = request.Result;
							if (response.StatusCode != HttpStatusCode.OK
								&& response.StatusCode != HttpStatusCode.Accepted)
								throw new RequestException(String.Format("Произошла ошибка при обработке запроса, код ошибки {0}", response.StatusCode),
									response.StatusCode);
							done = response.StatusCode == HttpStatusCode.OK;
							if (!done) {
								cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
								cancellation.ThrowIfCancellationRequested();
							}
						}

						if (response.StatusCode == HttpStatusCode.OK) {
							using (var file = File.OpenWrite(ArchiveFile)) {
								response.Content.ReadAsStreamAsync().Result.CopyTo(file);
							}
							Import(ArchiveFile);
						}
					}
				}
			}, cancellation);
		}

		private static void Import(string archiveFile)
		{
			List<Tuple<string, string[]>> data;
			using (var zip = new ZipFile(archiveFile)) {
				zip.ExtractAll(ExtractPath, ExtractExistingFileAction.OverwriteSilently);
				data = zip.GroupBy(z => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(z.FileName)))
					.Where(g => g.Count() == 2)
					.Select(g => Tuple.Create(
						Path.GetFullPath(Path.Combine(ExtractPath, g.First(z => !z.FileName.Contains(".meta.")).FileName)).Replace("\\", "/"),
						File.ReadAllLines(g.First(z => z.FileName.Contains(".meta.")).FileName)))
					.ToList();
			}
			using (var session = AppBootstrapper.NHibernate.Factory.OpenSession()) {
				var importer = new Importer(session);
				importer.Import(data);
			}
		}
	}
}