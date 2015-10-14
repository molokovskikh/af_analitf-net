using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using Common.Tools.Helpers;
using Devart.Data.MySql;
using Diadoc.Api.Http;
using DotRas;

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

		public static string TranslateException(Exception exception)
		{
			if (exception is HttpClientException) {
				var diadokException = ((HttpClientException)exception);
				var message = diadokException.AdditionalMessage;
				if (diadokException.DiadocErrorCode == "Http.Auth.Auth.UserNotFoundOrBadPassword")
					return "Введены некорректные учетные данные.";
				if (String.IsNullOrEmpty(message))
					return TranslateException(exception.InnerException);
				return message;
			}

			var webException = exception as WebException;
			if (webException?.Status == WebExceptionStatus.NameResolutionFailure) {
				return "Не удалось установить соединение с сервером Диадок. Проверьте подключение к Интернет.";
			}

			var requestException = exception as RequestException;
			if (requestException != null) {
				if (requestException.StatusCode == HttpStatusCode.Unauthorized) {
					return "Доступ запрещен.\r\nВведены некорректные учетные данные.";
				}
				if (requestException.StatusCode == HttpStatusCode.Forbidden) {
					return "Доступ запрещен.\r\nОбратитесь в АК Инфорум.";
				}
			}

			if ((exception is HttpRequestException
					&& exception.InnerException is WebException)
				|| (exception is TaskCanceledException
					&& !((TaskCanceledException)exception).CancellationToken.IsCancellationRequested)
				|| exception is RasException) {
				return "Не удалось установить соединение с сервером. Проверьте подключение к Интернет.";
			}

			var endUserError = exception as EndUserError;
			if (endUserError != null) {
				return endUserError.Message;
			}
			if (IsDbCorrupted(exception))
				return "База данных повреждена, используйте функцию" +
					" \"Восстановление базы данных\" из меню \"Сервис\" что бы починить базу данных.";
			if (exception.Chain().OfType<MySqlException>().Any(x => x.Code == 28))
				return "Недостаточно свободного места на диске для выполения операции.";

			return null;
		}

		public static bool IsDbCorrupted(Exception e)
		{
			//для некоторых ошибок есть два кода, один это официальный код mysql второй это код ошибки myisam
			//для embedded преобразование этих кодов не происходит
			var codes = new[] {
				//Incorrect information in file: '%s'
				1033,
				//Incorrect key file for table '%s'; try to repair it
				126, 1034,
				//Old key file for table '%s'; repair it!
				132, 1035,
				//Table '%s' is marked as crashed and should be repaired
				127, 144, 1194,
				//Table '%s' is marked as crashed and last (automatic?) repair failed
				145, 1195,
				//Table upgrade required. Please do "REPAIR TABLE `%s`" to fix it!
				164, 1459
			};
			return e.Chain().OfType<MySqlException>().Any(x => codes.Contains(x.Code));
		}

		//TaskCanceledException будет если пользователь нажал отмену и если время ожидания истекло
		public static bool IsCancalled(Exception baseException)
		{
			return baseException is TaskCanceledException
				&& ((TaskCanceledException)baseException).CancellationToken.IsCancellationRequested;
		}

		public static string TranslateIO(IOException exception)
		{
			if (exception is FileNotFoundException) {
				return $"Файл {exception.Message} не найден";
			}
			return exception.Message;
		}
	}
}