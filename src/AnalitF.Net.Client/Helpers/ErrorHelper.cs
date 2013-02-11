using System;
using System.Net;
using AnalitF.Net.Client.Models;

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
			var requestException = exception.GetBaseException() as RequestException;
			if (requestException != null) {
				if (requestException.StatusCode == HttpStatusCode.Unauthorized) {
					return "Доступ запрещен.\r\nВведены некорректные учетные данные.";
				}
				if (requestException.StatusCode == HttpStatusCode.Forbidden) {
					return "Доступ запрещен.\r\nОбратитесь в АК Инфорум.";
				}
			}

			var endUserError = exception.GetBaseException() as EndUserError;
			if (endUserError != null) {
				return endUserError.Message;
			}
			return null;
		}
	}
}