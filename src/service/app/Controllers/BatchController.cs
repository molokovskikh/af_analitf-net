﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
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
	public class BatchController : JobController2
	{
		public HttpResponseMessage Post(HttpRequestMessage request)
		{
			var input = request.Content.ReadAsStreamAsync().Result;
			BatchRequest requestMeta;
			Stream payloadStream;
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

			return StartJob((session, config, job) => {
				try {
					List<Order> orders;
					List<OrderBatchItem> batchItems;
					var batchAddress = session.Load<Address>(requestMeta.AddressId);
					using (payloadStream) {
						var handler = new SmartOrderBatchHandler(job.User, batchAddress, payloadStream);
						orders = handler.ProcessOrderBatch();
						batchItems = handler.OrderBatchItems;
						orders.Each(o => o.RowId = (uint)o.GetHashCode());
						orders.SelectMany(o => o.OrderItems).Each(o => o.RowId = (uint)o.GetHashCode());
					}

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