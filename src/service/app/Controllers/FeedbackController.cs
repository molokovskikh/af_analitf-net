using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using Common.Models;
using NHibernate;
using Attachment = System.Net.Mail.Attachment;

namespace AnalitF.Net.Service.Controllers
{
	public class FeedbackController : ApiController
	{
		public Config.Config Config;
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public HttpResponseMessage Post(FeedbackMessage message)
		{
			var mail = new MailMessage();
			mail.From = new MailAddress("afmail@analit.net", String.Format("Код пользователя {0} код клиента {1} [{2}]",
				CurrentUser.Id, CurrentUser.Client.Id, CurrentUser.Client.Name));
			if (message.IsBilling)
				mail.To.Add(new MailAddress(Config.BillingMail));
			else if (message.IsOffice)
				mail.To.Add(new MailAddress(Config.OfficeMail));
			else
				mail.To.Add(new MailAddress(Config.SupportMail));
			mail.Subject = message.Subject;
			mail.Body = message.Body;
			if (message.Attachments != null && message.Attachments.Length > 0)
				mail.Attachments.Add(new Attachment(new MemoryStream(message.Attachments), "Вложения.zip"));
			var client = new SmtpClient();
			client.Send(mail);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}
	}
}