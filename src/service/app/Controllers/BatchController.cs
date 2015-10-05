using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using AnalitF.Net.Service.Models;
using Common.Models;
using Common.Tools;
using Ionic.Zip;
using Newtonsoft.Json;
using SmartOrderFactory;
using SmartOrderFactory.Domain;

namespace AnalitF.Net.Service.Controllers
{
	public class BatchController : JobController2
	{
		public HttpResponseMessage Post(HttpRequestMessage request)
		{
			BatchRequest requestMeta;
			Stream payloadStream = null;
			if (request.Content.Headers.ContentType != null
				&& request.Content.Headers.ContentType.MediaType.Match("application/json")) {
				requestMeta = request.Content.ReadAsAsync<BatchRequest>().Result;
			}
			else {
				var input = request.Content.ReadAsStreamAsync().Result;
				using(var zip = ZipFile.Read(input)) {
					var meta = zip.Entries.First(e => e.FileName.Match("meta.json"));
					var serializer = new JsonSerializer();
					using(var streamReader = new StreamReader(meta.OpenReader())) {
						requestMeta = (BatchRequest)serializer.Deserialize(streamReader, typeof(BatchRequest));
					}
					var payload = zip.Entries.First(e => e.FileName.Match("payload"));
					payloadStream = FileHelper.SelfDeleteTmpFile();
					payload.Extract(payloadStream);
					payloadStream.Position = 0;
				}
			}

			return StartJob((session, config, job) => {
				job.LastSync = requestMeta.LastSync;
				try {
					var batchAddress = session.Load<Address>(requestMeta.AddressId);
					SmartOrderBatchHandler handler;
					if (payloadStream == null) {
						var items = requestMeta.BatchItems.Select(i => new OrderBatchItem(i.ProductName, i.ProducerName, i.Quantity) {
							Code = i.Code,
							CodeCr = i.CodeCr,
							SupplierDeliveryId = i.SupplierDeliveryId,
							ServiceValues = i.ServiceValues,
							Priority = i.Priority,
							BaseCost = i.BaseCost
						}).ToList();
						handler = new SmartOrderBatchHandler(session, job.User, batchAddress, items);
						handler.RespectLimits = true;
					}
					else {
						using (payloadStream) {
							handler = new SmartOrderBatchHandler(session, job.User, batchAddress, payloadStream);
						}
					}
					handler.JunkPeriod = requestMeta.JunkPeriod;
					var orders = handler.ProcessOrderBatch();
					var batchItems = handler.OrderBatchItems;
					orders.Each(o => o.RowId = (uint)o.GetHashCode());
					orders.SelectMany(o => o.OrderItems).Each(o => o.RowId = (uint)o.GetHashCode());

					using(var exporter = new Exporter(session, config, job)) {
						exporter.Orders = orders;
						exporter.BatchItems = batchItems;
						exporter.BatchAddress = batchAddress;
						exporter.ExportAll();
						exporter.Compress(job.OutputFile(Config));
					}
				}
				catch(SmartOrderException e) {
					Log.Warn("Ошибка при обработке автозаказа", e);
					job.ErrorDescription = e.Message;
					job.Faulted(e);
				}
				catch(ExcelException e) {
					Log.Warn("Ошибка при обработке автозаказа", e);
					job.ErrorDescription = "Не удалось разобрать файл дефектуры, проверьте формат файла.";
					job.Faulted(e);
				}
				catch(XmlException e) {
					Log.Warn("Ошибка при обработке автозаказа", e);
					job.ErrorDescription = "Не удалось разобрать файл дефектуры, проверьте формат файла.";
					job.Faulted(e);
				}
				catch(DbfException e) {
					Log.Warn("Ошибка при обработке автозаказа", e);
					job.ErrorDescription = "Не удалось разобрать файл дефектуры, проверьте формат файла.";
					job.Faulted(e);
				}
				catch(DuplicateNameException e) {
					Log.Warn("Ошибка при обработке автозаказа", e);
					job.ErrorDescription = "Не удалось разобрать файл дефектуры, проверьте формат файла.";
					job.Faulted(e);
				}
				catch(IndexOutOfRangeException e) {
					//это исключение возникнет в DefaultSource если на вход сунуть какую нибудь ерунду
					Log.Warn("Ошибка при обработке автозаказа", e);
					job.ErrorDescription = "Не удалось разобрать файл дефектуры, проверьте формат файла.";
					job.Faulted(e);
				}
			});
		}
	}
}