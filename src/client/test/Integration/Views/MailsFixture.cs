using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class MailsFixture : DispatcherFixture
	{
		[Test]
		public void Search_fixture()
		{
			var subject = Guid.NewGuid().ToString();
			session.Save(new Mail(subject));

			Open();
			Input("Term", subject);
			scheduler.AdvanceByMs(1000);
			WaitIdle();
			AssertItemsCount("Items", 1);
		}

		[Test]
		public void Sort_fixture()
		{
			session.DeleteEach(session.Query<Mail>().Where(m => !m.Subject.Contains("subject")));
			session.Save(new Mail("тестовая тема 1") {
				SentAt = DateTime.Today.AddDays(-10)
			});
			session.Save(new Mail("тестовая тема 2") {
				SentAt = DateTime.Today.AddDays(-1)
			});

			Open();
			Toggle("IsAsc");
			dispatcher.Invoke(() => {
				var sort = Find<ComboBox>("Sort");
				sort.SelectedItem = "Сортировка: Тема";
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var list = Find<ListView>("Items");
				var items = list.Items.OfType<Mail>().Where(m => (m.Subject ?? "").StartsWith("тестовая тема")).ToArray();
				Assert.AreEqual("тестовая тема 1", items[0].Subject);
				Assert.AreEqual("тестовая тема 2", items[1].Subject);
			});
		}

		[Test]
		public void Load_attachment()
		{
			var attachment = Download();
			WaitDownloaded(attachment);
			WaitIdle();
			dispatcher.Invoke(() => {
				var attachments = Find<ItemsControl>("CurrentItem_Value_Attachments");
				var button = attachments.Descendants<Button>().First();
				Assert.AreEqual("downloaded", button.Tag);
			});
		}


		[Test]
		public void Cancel_loading()
		{
			Env.RequestDelay = 10.Second();
			var attachment = Download();
			Assert.IsTrue(attachment.IsDownloading);
			WaitIdle();
			dispatcher.Invoke(() => {
				var attachments = Find<ItemsControl>("CurrentItem_Value_Attachments");
				var button = attachments.Descendants<Button>().First();
				Assert.AreEqual("downloading", button.Tag);
				InternalClick(button);
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var attachments = Find<ItemsControl>("CurrentItem_Value_Attachments");
				var button = attachments.Descendants<Button>().First();
				Assert.AreEqual("wait", button.Tag);
			});

			//проверяем что анимация загрузки завершилась в случае отмены
			scheduler.Start();
			//даем возможность начать анимацию
			WaitIdle();
			dispatcher.Invoke(() => {
				var button = Find<Button>("ShowJournal");
				var p = button.Descendants<System.Windows.Shapes.Path>().First();
				Assert.IsFalse(DependencyPropertyHelper.GetValueSource(p.RenderTransform, TranslateTransform.YProperty).IsAnimated);
			});
			Assert.AreEqual(0, shell.PendingDownloads.Count);
		}

		[Test]
		public void Change_active_while_download()
		{
			Env.RequestDelay = 10.Second();
			var subject = Guid.NewGuid().ToString();
			var randomMail = new Mail(subject);
			session.Save(randomMail);

			var attachment = Download();
			SelectById("Items", randomMail.Id);
			WaitIdle();
			SelectByAttachmentId(attachment.Id);
			WaitIdle();

			dispatcher.Invoke(() => {
				var attachments = Find<ItemsControl>("CurrentItem_Value_Attachments");
				var button = attachments.Descendants<Button>().First();
				Assert.AreEqual("downloading", button.Tag);
			});
		}

		[Test]
		public void Close_form_while_download()
		{
			Env.RequestDelay = 0.1.Second();
			var attachment = Download();
			scheduler.AdvanceByMs(100);
			Click("ShowMain");
			Click("ShowMails");
			var localAttachment = SelectByAttachmentId(attachment.Id);
			WaitDownloaded(localAttachment);
			WaitIdle();
			dispatcher.Invoke(() => {
				var attachments = Find<ItemsControl>("CurrentItem_Value_Attachments");
				var button = attachments.Descendants<Button>().First();
				Assert.AreEqual("downloaded", button.Tag);
			});
		}

		[Test]
		public void Delete_mails()
		{
			var subject = Guid.NewGuid().ToString();
			var mail = new Mail(subject);
			session.Save(mail);

			Open();
			dispatcher.Invoke(() => {
				var items = activeWindow.Descendants<ListView>().First(c => c.Name == "Items");
				items.SelectedItems.Add(items.Items.OfType<Mail>().First(m => m.Subject == subject));
			});
			Click("Delete");
			dispatcher.Invoke(() => {
				var items = activeWindow.Descendants<ItemsControl>().First(c => c.Name == "Items");
				Assert.IsFalse(items.Items.OfType<Mail>().All(m => m.Subject == subject));
			});
			Click("ShowMain");
			session.Clear();
			Assert.IsNull(session.Get<Mail>(mail.Id));
		}

		[Test]
		public void Set_marker()
		{
			var subject = Guid.NewGuid().ToString();
			var mail = new Mail(subject);
			session.Save(mail);
			Open();

			var item = SelectById("Items", mail.Id);
			Toggle("CurrentItem_Value_IsImportant");

			Assert.IsTrue(item.IsImportant);
		}

		private Mail SelectById(string name, uint id)
		{
			Mail result = null;
			dispatcher.Invoke(() => {
				var items = activeWindow.Descendants<Selector>().First(c => c.Name == name);
				result = items.Items.OfType<Mail>().First(m => m.Id == id);
				items.SelectedItem = result;
			});
			return result;
		}

		private static void WaitDownloaded(Attachment attachment)
		{
			Assert.IsTrue(attachment.IsDownloading);
			Assert.IsFalse(attachment.IsDownloaded);
			attachment.Changed().Timeout(10.Second()).First(c => c.EventArgs.PropertyName == "IsDownloading");
			Assert.IsNull(attachment.Exception);
			Assert.IsFalse(attachment.IsError, attachment.ToString());
			Assert.IsTrue(attachment.IsDownloaded, attachment.ToString());
		}

		private Attachment Download()
		{
			return StartDownload(SetupDownload());
		}

		private Attachment StartDownload(Attachment att)
		{
			var attachment = SelectByAttachmentId(att.Id);
			WaitIdle();
			dispatcher.Invoke(() => {
				var attachments = Find<ItemsControl>("CurrentItem_Value_Attachments");
				var button = attachments.Descendants<Button>().First();
				InternalClick(button);
			});
			return attachment;
		}

		private Attachment SetupDownload()
		{
			var att = session.Query<Attachment>().First(a => a.Name == "отказ.txt");
			att.IsDownloaded = false;

			Start();
			Env.Scheduler = new MixedScheduler(scheduler, new DispatcherScheduler(dispatcher));
			Click("ShowMails");
			return att;
		}

		private Attachment SelectByAttachmentId(uint attachmentId)
		{
			Attachment attachment = null;
			dispatcher.Invoke(() => {
				var items = Find<ListView>("Items");
				var mail = items.Items.Cast<Mail>().First(m => m.Attachments.Any(a => a.Id == attachmentId));
				attachment = mail.Attachments.First(a => a.Id == attachmentId);
				items.SelectedItem = mail;
			});
			return attachment;
		}

		private void Toggle(string name)
		{
			dispatcher.Invoke(() => {
				var b = activeWindow.Descendants<ToggleButton>().First(c => c.Name == name);
				b.IsChecked = !b.IsChecked;
			});
		}

		public T Find<T>(string name) where T : FrameworkElement
		{
			return activeWindow.Descendants<T>().First(c => c.Name == name);
		}

		private void Open()
		{
			Start();
			Click("ShowMails");
		}

		private void AssertItemsCount(string name, int count)
		{
			dispatcher.Invoke(() => {
				var items = activeWindow.Descendants<ItemsControl>().First(c => c.Name == name);
				Assert.AreEqual(count, items.Items.Count);
			});
		}
	}
}