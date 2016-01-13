using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using log4net;
using Newtonsoft.Json.Linq;

namespace AnalitF.Net.Client.Models.Sbis
{
	public class User
	{
		public string Идентификатор;
		public string Имя;
		public string Отчество;
		public string Фамилия;

		public override string ToString() => $"{Фамилия} {Имя} {Отчество}";
	}

	public class Status
	{
		public string Код;
		public string Название;
		public string Описание;
		public string Примечание;
	}

	public class FileRef
	{
		public string Имя;
		public string Ссылка;
		public string Хеш;
		public string ДвоичныеДанные;

		public FileRef()
		{
		}

		public FileRef(byte[] signature)
		{
			ДвоичныеДанные = Convert.ToBase64String(signature);
		}
	}

	public class Attach
	{
		public string Идентификатор;
		public string Название;
		public string СсылкаНаPDF;
		public FileRef Файл;
		public FileRef[] Подпись;
	}

	public class OrgRef
	{
		public OrgId СвЮЛ;
		public string Email;
		public string Телефон;
	}

	public class OrgId
	{
		public string ИНН;
		public string КПП;
		public string Название;
	}

	public class Doc
	{
		public Attach[] Вложение;
		public string ДатаВремя;
		public OrgRef Контрагент;
		public string Название;
		public string Направление;
		public OrgRef НашаОрганизация;
		public string Номер;
		public string Примечание;
		public Status Состояние;
		public User Ответственный;
		public string Идентификатор;
	}

	public class DocRef
	{
		public string ДатаВремя;
		public Doc Документ;
	}

	public class Nav
	{
		public int РазмерСписка;
		public int РазмерСтраницы;
	}

	public class Result
	{
		public DocRef[] Реестр;
		public Nav Навигация;
	}

	public static class RpcHelper
	{
		private static ILog log  = LogManager.GetLogger(typeof(RpcHelper));

		public static async Task<JObject> JsonRpc(this HttpClient client, string method, object payload)
		{
			var url = "https://online.sbis.ru/service/";
			if (method == "СБИС.Аутентифицировать")
				url = "https://online.sbis.ru/auth/service/";
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
			var json = JObject.Parse(await result.Content.ReadAsStringAsync());
			var error = json["error"];
			if (error != null) {
				log.Error($"Ошибка при вызове метода {method} {error}");
				throw new EndUserError(error["details"]?.ToString() ?? error["message"]?.ToString());
			}
			return json;
		}
	}
}