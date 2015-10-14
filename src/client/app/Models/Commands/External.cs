using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class External
	{
		private static ILog Log = LogManager.GetLogger(typeof(External));

		public static async Task Sbis()
		{
			using (var client = new HttpClient()) {
				var result = await client.JsonRpc("СБИС.Аутентифицировать", new {
					Логин = "поставщик",
					Пароль = "поставщик123"
				});
				client.DefaultRequestHeaders.Add("X-SBISSessionID", result["result"].ToObject<string>());
				result = await client.JsonRpc("СБИС.СписокДокументовПоСобытиям", new {
					Фильтр = new
					{
						ТипРеестра = "Входящие",
						//ДатаС = "04.07.2014",
						//Тип = "ДокОтгрВх"
						//Тип = "НакладнаяВх",
						//Состояние = "Есть документ",
						//Направление = "Входящий",
						Навигация = new
						{
							РазмерСтраницы = "10",
							Страница = "0",
							ВернутьРазмерСписка = "Да"
						}
					}
				});
				Console.WriteLine(result);
			}
		}
	}

	public static class RpcHelper
	{
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
			if (error != null)
				throw new Exception(error.ToString());
			return json;
		}
	}
}