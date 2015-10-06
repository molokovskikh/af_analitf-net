using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using Diadoc.Api;
using Diadoc.Api.Com;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto.Documents;
using Diadoc.Api.Proto.Events;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using AttachmentType = Diadoc.Api.Proto.Events.AttachmentType;
using EntityType = Diadoc.Api.Proto.Events.EntityType;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Models.Commands
{
	public class External
	{
		private static ILog Log = LogManager.GetLogger(typeof(External));

		public static void Diadok(ISession session, Config.Config config)
		{
			var results = new List<IResult>();
			var settings = session.Query<Settings>().First();
			if (String.IsNullOrEmpty(settings.DiadokUsername) || String.IsNullOrEmpty(settings.DiadokPassword))
				return;
			var api = new DiadocApi(config.DiadokApiKey, config.DiadokUrl, new WinApiCrypt());
			var token = api.Authenticate(settings.DiadokUsername, settings.DiadokPassword);

			var toSign = session.Query<Waybill>().Where(x => x.DocType == DocType.Diadok && x.IsSign && !x.IsSigned)
				.ToArray();
			foreach (var waybill in toSign) {
				var patch = new MessagePatchToPost {
					BoxId = waybill.DiadokBoxId,
					MessageId = waybill.DiadokMessageId
				};
				var sign = new DocumentSignature {
					ParentEntityId = waybill.DiadokEnityId,
				};
				if (settings.DebugUseTestSign) {
					sign.SignWithTestSignature = true;
				}
				else {
					sign.Signature = session.Load<Sign>(waybill.Id).SignBytes;
				}
				patch.AddSignature(sign);
				Log.InfoFormat("Попытка подписать вложение {0} документа {1}", waybill.DiadokEnityId, waybill.DiadokMessageId);
				try {
					api.PostMessagePatch(token, patch);
					waybill.IsSigned = true;
					var fullname = waybill.TryGetFile(settings);
					session.Save(new JournalRecord {
						CreateAt = DateTime.Now,
						Name = $"Подписан документ Диадок {waybill.Filename}",
						Filename = fullname
					});
					Log.InfoFormat("Подписано вложение {0} документа {1}", waybill.DiadokEnityId, waybill.DiadokMessageId);
				}
				catch(WebException e) {
					var http = e.Response as HttpWebResponse;
					if (http != null && (int)http.StatusCode == 409) {
						Log.Warn($"Не удалось подписать документ {waybill.Filename}. Документ уже подписан.", e);
						results.Add(MessageResult.Error("Не удалось подписать документ {0}. Документ уже подписан.", waybill.Filename));
					}
					else {
						throw;
					}
				}
			}

			var boxes = api.GetMyOrganizations(token).Organizations.SelectMany(x => x.Boxes);
			foreach (var box in boxes) {
				var documentsFilter = new DocumentsFilter {
					FilterCategory = "Any.Inbound",
					BoxId = box.BoxId,
					SortDirection = "Descending"
				};

				DocumentList documents;
				do {
					documents = api.GetDocuments(token, documentsFilter);
					var endFound = false;
					foreach (var doc in documents.Documents.Where(x => !x.IsDeleted)) {
						documentsFilter.AfterIndexKey = doc.IndexKey;
						if (session.Query<Waybill>().Any(x => x.DiadokMessageId == doc.MessageId)) {
							endFound = true;
							break;
						}

						var supplier = session.Query<Supplier>().FirstOrDefault(x => x.DiadokOrgId == doc.CounteragentBoxId);
						var message = api.GetMessage(token, box.BoxId, doc.MessageId);
						var files = message.Entities
							.Where(e => e.EntityType == EntityType.Attachment
								&& !String.IsNullOrEmpty(e.FileName)
								&& e.AttachmentType == AttachmentType.Nonformalized);
						foreach (var file in files) {
							var waybill = new Waybill {
								DiadokMessageId = doc.MessageId,
								DiadokBoxId = box.BoxId,
								DiadokEnityId = file.EntityId,
								DocType = DocType.Diadok,
								Filename = file.FileName,
								//подписывать надо только если документ не был подписан ранее и требует подписи
								IsSigned = doc.NonformalizedDocumentMetadata.DocumentStatus
									!= Diadoc.Api.Proto.Documents.NonformalizedDocument.NonformalizedDocumentStatus.InboundWaitingForRecipientSignature,
								IsNew = true,
								WriteTime = DateTime.Now,
								ProviderDocumentId = String.IsNullOrEmpty(doc.CustomDocumentId) ? doc.MessageId : doc.CustomDocumentId,
								DocumentDate = NullableConvert.ToDateTime(doc.DocumentDate).GetValueOrDefault(doc.CreationTimestamp),
								Supplier = supplier,
								UserSupplierName = supplier == null ? message.FromTitle : null,
							};
							session.Save(waybill);
							var path = settings.MapPath("Waybills");
							Directory.CreateDirectory(path);
							var fullname = Path.Combine(path, waybill.Id + "_" + FileHelper.StringToFileName(file.FileName));
							if (fullname.Length > 260)
								fullname = Path.Combine(path, waybill.Id + Path.GetExtension(file.FileName));
							File.WriteAllBytes(fullname, file.Content.Data);
							Log.InfoFormat($"Получен документ Диадок {doc.MessageId} файл {file.FileName}");
							session.Save(new JournalRecord($"Загружен документ Диадок {file.FileName}", fullname));
						}
					}
					if (endFound)
						break;
				} while (documents.TotalCount > documents.Documents.Count);
			}
		}//public static void Diadok

		public static async void Sbis()
		{
			using (var client = new HttpClient()) {
				var result = await client.JsonRpc("СБИС.Аутентифицировать", new {
					Логин = "покупатель",
					Пароль = "покупатель123"
				});
				client.DefaultRequestHeaders.Add("X-SBISSessionID", result["result"].ToObject<string>());
				result = await client.JsonRpc("СБИС.СписокДокументов", new {
					Тип = "НакладнаяВх",
					Состояние = "Есть документ",
					Направление = "Входящий",
					Навигация = new {
						РазмерСтраницы = 10,
						Страница = 0,
						ВернутьРазмерСписка = "Да"
					}
				});
				var docs = result["result"]["Документ"].ToObject<JArray>();
				foreach (var doc in docs) {
					Console.WriteLine(doc);
				}//foreach (var doc in docs)
			}//using (var client = new HttpClient())
		}
	}

	public static class RpcHelper
	{
		public static async Task<JObject> JsonRpc(this HttpClient client, string method, object payload)
		{
			var url = "https://online.sbis.ru/auth/service/";
			var content = new ObjectContent<object>(new {
					jsonrpc = "2.0",
					method = method,
					@params = payload,
					id = 0
			}, new JsonMediaTypeFormatter());
			content.Headers.ContentType = new MediaTypeHeaderValue("application/json-rpc") {
				CharSet = "utf-8"
			};
			var result = await client.PostAsync(url, content);
			return JObject.Parse(await result.Content.ReadAsStringAsync());
		}
	}
}