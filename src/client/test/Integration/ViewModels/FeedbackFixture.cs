using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Service.Test.TestHelpers;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class FeedbackFixture
	{
		private FileCleaner cleaner;
		private CompositeDisposable trash = new CompositeDisposable();
		private Feedback feedback;

		[SetUp]
		public void Setup()
		{
			feedback = new Feedback();
			cleaner = new FileCleaner();
			trash.Add(cleaner);
		}

		[TearDown]
		public void TearDown()
		{
			trash.Dispose();
		}

		[Test]
		public void Get_message_with_attachments()
		{
			trash.Add(feedback);

			var result = feedback.AddAttachment().GetEnumerator();
			result.MoveNext();
			var randomFile = cleaner.RandomFile();
			File.WriteAllText(randomFile, "123");
			((OpenFileResult)result.Current).Dialog.FileName = randomFile;
			result.MoveNext();
			Assert.AreEqual(1, feedback.Attachments.Count);

			result = feedback.Send().GetEnumerator();
			RunTask(result);
			Assert.IsNotNull(feedback.ArchiveName);
			var message = feedback.GetMessage();
			Assert.IsNull(feedback.ArchiveName);

			Assert.IsNotNull(message);
			Assert.That(message.Attachments.Length, Is.GreaterThan(0));
		}

		[Test]
		public void Get_message_without_attachments()
		{
			Directory.GetFiles(".", "*.log").Each(File.Delete);
			cleaner.Watch("test.log");
			File.WriteAllText("test.log", "test");
			trash.Add(feedback);
			var result = feedback.Send().GetEnumerator();
			RunTask(result);
			var message = feedback.GetMessage();
			Assert.IsNull(feedback.ArchiveName);
			Assert.AreEqual("test.log", ZipHelper.lsZip(message.Attachments).Implode());
		}

		private static void RunTask(IEnumerator<IResult> result)
		{
			Assert.IsTrue(result.MoveNext());
			var task = ((TaskResult)result.Current).Task;
			task.Start();
			task.Wait();
		}
	}
}