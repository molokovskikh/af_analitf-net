using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;

namespace AnalitF.Net.Client.Helpers
{
	public class EndUserError : Exception
	{
		public EndUserError(string message) : base(message)
		{
		}
	}

	public class ErrorHelper
	{
		public static string TranslateException(AggregateException exception)
		{
			return TranslateException(exception.GetBaseException());
		}

		public static string TranslateException(Exception baseException)
		{
			var requestException = baseException as RequestException;
			if (requestException != null) {
				if (requestException.StatusCode == HttpStatusCode.Unauthorized) {
					return "Доступ запрещен.\r\nВведены некорректные учетные данные.";
				}
				if (requestException.StatusCode == HttpStatusCode.Forbidden) {
					return "Доступ запрещен.\r\nОбратитесь в АК Инфорум.";
				}
			}

			if (baseException is HttpRequestException
				&& baseException.InnerException is WebException
				&& baseException.InnerException.InnerException is SocketException) {
				return "Не удалось установить соединение с сервером. Проверьте подключение к Интернет.";
			}

			var endUserError = baseException as EndUserError;
			if (endUserError != null) {
				return endUserError.Message;
			}
			return null;
		}
	}
}