using System;
using System.IO;
using System.Net.Http;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Models.Commands
{
	public class SendFeedback : RemoteCommand
	{
		private Feedback feedback;

		public SendFeedback(Feedback feedback)
		{
			this.feedback = feedback;
			ErrorMessage = "Не отправить письмо. Попробуйте повторить операцию позднее.";
			SuccessMessage = "Письмо отправлено.";
		}

		protected override UpdateResult Execute()
		{
			Progress.OnNext(new Progress("Соединение", 100, 0));
			Progress.OnNext(new Progress("Отправка", 0, 50));
			var message = feedback.GetMessage();
			var response = Client.PostAsJsonAsync("Feedback", message, Token).Result;
			CheckResult(response);
			Progress.OnNext(new Progress("Отправка", 100, 100));
			Results.Add(new MessageResult("Письмо отправлено."));
			return UpdateResult.NotReload;
		}
	}
}