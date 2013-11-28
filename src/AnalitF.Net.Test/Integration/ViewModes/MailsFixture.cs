using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Web.UI.WebControls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Common.Tools.Calendar;
using log4net.Config;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class MailsFixture : ViewModelFixture
	{
		[Test]
		public void Download_attachment()
		{
			var att = session.Query<Attachment>().First(a => a.Name == "отказ.txt");
			att.IsDownloaded = false;

			BaseScreen.TestSchuduler = ImmediateScheduler.Instance;
			var mails = Init<Mails>();
			var attachment = mails.Items.Value.SelectMany(m => m.Attachments).First(a => !a.IsDownloaded);

			//что бы выполнить запланированную задачу
			mails.Download(attachment);
			var changes = attachment.Changed().Select(e => e.EventArgs.PropertyName).Collect();
			var results = mails.ResultsSink.Collect();
			attachment.Changed().Timeout(10.Second())
				.First(p => p.EventArgs.PropertyName == "IsDownloaded" || p.EventArgs.PropertyName == "IsError");

			Assert.That(changes, Has.Some.EqualTo("Progress"));
			Assert.IsTrue(attachment.IsDownloaded);
			Assert.IsFalse(attachment.IsError);
			Assert.IsFalse(attachment.IsDownloading);
			Assert.IsFalse(attachment.IsConnecting);

			Assert.IsNotNull(attachment.FileTypeIcon);
			Assert.IsNotNull(attachment.LocalFilename);
			Assert.AreEqual(1, results.Count);
			Assert.IsInstanceOf<OpenResult>(results[0]);
		}

		[Test]
		public void Mail_stat()
		{
			var mail = session.Query<Mail>().First();
			mail.IsNew = true;
			session.Save(mail);

			var mails = Init<Mails>();
			var value = shell.NewMailsCount.Value;
			Assert.That(value, Is.GreaterThan(0));
			mails.CurrentItem.Value = mails.Items.Value.First(m => m.Id == mail.Id);
			testScheduler.AdvanceByMs(10000);
			Assert.AreEqual(value - 1, shell.NewMailsCount.Value);
		}
	}
}