using System.IO;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using Test.Support.Documents;
using Test.Support.Logs;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class MailWithAttachment : ServerFixture
	{
		public TestMail Mail;
		public bool IsSpecial;

		public override void Execute(ISession session)
		{
			var user = User(session);
			var supplier = user.GetActivePrices(session)[0].Supplier;
			Mail = new TestMail(supplier);
			Mail.Attachments.Add(new TestAttachment(Mail, "отказ.txt"));
			if (IsSpecial) {
				Mail.IsVIPMail = true;
				Mail.SupplierEmail = "test@analit.net";
			}
			session.Save(Mail);
			session.Save(new TestMailSendLog(user, Mail));
			File.WriteAllText(Path.Combine(Config.AttachmentsPath, Mail.Attachments[0].GetSaveFileName()), "тест");
		}
	}
}