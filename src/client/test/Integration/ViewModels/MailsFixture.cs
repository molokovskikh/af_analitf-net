using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Common.Tools.Calendar;
using log4net.Appender;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class MailsFixture : ViewModelFixture<Mails>
	{
		QueryCatcher catcher;

		[SetUp]
		public void Setup()
		{
			catcher = new QueryCatcher("AnalitF.Net");
			catcher.Appender = new MemoryAppender();
			catcher.Start();
		}

		[TearDown]
		public void TearDown()
		{
			if (DbHelper.IsTestFail())
				Console.WriteLine(((MemoryAppender)catcher.Appender).GetEvents().Implode(e => e.MessageObject));
		}

		[Test]
		public void Download_attachment()
		{
			var attachment = Download();
			var changes = attachment.Changed().Select(e => e.EventArgs.PropertyName).Collect();
			var results = model.ResultsSink.Collect();
			attachment.Changed().Timeout(10.Second())
				.First(p => p.EventArgs.PropertyName == "IsDownloaded");

			Assert.That(changes, Has.Some.EqualTo("Progress"));
			Assert.IsTrue(attachment.IsDownloaded);
			Assert.IsFalse(attachment.IsError);
			Assert.IsFalse(attachment.IsDownloading);
			Assert.IsFalse(attachment.IsConnecting);

			Assert.IsNotNull(attachment.FileTypeIcon);
			Assert.That(attachment.LocalFilename, Is.Not.Null.Or.Empty);
			Assert.IsTrue(model.IsActive);
			Assert.AreEqual(1, results.Count);
			Assert.IsInstanceOf<OpenResult>(results[0]);
		}

		[Test]
		public void Mail_stat()
		{
			var mail = session.Query<Mail>().First();
			mail.IsNew = true;
			session.Save(mail);

			var value = shell.NewMailsCount.Value;
			Assert.That(value, Is.GreaterThan(0));
			model.CurrentItem.Value = model.Items.Value.First(m => m.Id == mail.Id);
			scheduler.AdvanceByMs(10000);
			Assert.IsFalse(model.CurrentItem.Value.IsNew);
			Assert.AreEqual(value - 1, shell.NewMailsCount.Value);
		}

		[Test]
		public void Complete_download_on_close()
		{
			var attachment = Download();
			Close(model);

			var message = WaitNotification();
			Assert.AreEqual("Файл 'отказ.txt' загружен", message);

			session.Clear();
			attachment = session.Get<Attachment>(attachment.Id);
			Assert.IsNotNull(attachment.LocalFilename);
			Assert.IsTrue(attachment.IsDownloaded);
		}

		[Test]
		public void Delete_mail_with_attachment()
		{
			var mail = new Mail("тест");
			var attachment = new Attachment("test.txt", 1);
			mail.Attachments.Add(attachment);
			session.Save(mail);
			attachment.UpdateLocalFile(attachment.Id.ToString());
			File.WriteAllText(attachment.LocalFilename, "1");

			model.SelectedItems.Add(model.Items.Value.First(m => m.Id == mail.Id));
			model.Delete();
			Assert.IsFalse(File.Exists(attachment.LocalFilename), attachment.LocalFilename);
		}

		[Test]
		public void Search_with_empty_mail()
		{
			session.Save(new Mail());
			Assert.That(model.Items.Value.Count, Is.GreaterThan(0));
			model.Term.Value = "тasdasест";
			scheduler.AdvanceByMs(1000);
			Assert.AreEqual(0, model.Items.Value.Count);
		}

		[Test]
		public void Reassociate_pending_attachments()
		{
			Env.Barrier = new Barrier(2);
			var attachment = Download();
			Close(model);
			scheduler.Start();
			Assert.AreEqual(1, shell.PendingDownloads.Count);
			shell.ShowMails();
			var reloaded = model.Items.Value.SelectMany(m => m.Attachments).First(a => a.Id == attachment.Id);
			var changes = reloaded.Changed().Select(e => e.EventArgs.PropertyName).Collect();

			Assert.IsTrue(reloaded.IsDownloading);
			Assert.IsTrue(Env.Barrier.SignalAndWait(10.Second()), "не удалось дождаться загрузки");
			WaitNotification();
			Assert.IsTrue(reloaded.IsDownloaded, $"{reloaded} {reloaded.GetHashCode()}");
			Assert.That(changes, Has.Some.EqualTo("IsDownloaded"));
		}

		[Test]
		public void Update_jounal_on_download_complete()
		{
			Env.Barrier = new Barrier(2);
			var attachment = Download();
			Close(attachment);
			shell.ShowJournal();
			var journal = (Journal)shell.ActiveItem;
			var oldCount = journal.Items.Value.Count;
			Assert.IsTrue(Env.Barrier.SignalAndWait(10.Second()), "не удалось дождаться загрузки");
			WaitNotification();
			scheduler.Start();
			Assert.That(journal.Items.Value.Count, Is.GreaterThan(oldCount));
		}

		private Attachment Download()
		{
			//что бы выполнить запланированную задачу
			//todo это как то криво лучше не переопределять планировщики а запускать тестовый
			Env.Scheduler = ImmediateScheduler.Instance;
			Env.UiScheduler = ImmediateScheduler.Instance;

			var att = session.Query<Attachment>().First(a => a.Name == "отказ.txt");
			att.IsDownloaded = false;

			var attachment = model.Items.Value.SelectMany(m => m.Attachments).First(a => !a.IsDownloaded);
			model.Download(attachment);
			return attachment;
		}
	}
}