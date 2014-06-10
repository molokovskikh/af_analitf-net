﻿using System.IO;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using Microsoft.Win32;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class FeedbackFixture
	{
		private FileCleaner cleaner;
		private CompositeDisposable trash = new CompositeDisposable();

		[SetUp]
		public void Setup()
		{
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
			var feedback = new Feedback();
			trash.Add(feedback);

			var result = feedback.AddAttachment().GetEnumerator();
			result.MoveNext();
			var randomFile = cleaner.RandomFile();
			File.WriteAllText(randomFile, "123");
			((OpenFileResult)result.Current).Dialog.FileName = randomFile;
			result.MoveNext();
			Assert.AreEqual(1, feedback.Attachments.Count);

			result = feedback.Send().GetEnumerator();
			result.MoveNext();
			var task = ((TaskResult)result.Current).Task;
			task.Start();
			task.Wait();
			Assert.IsNotNull(feedback.ArchiveName);
			var message = feedback.GetMessage();
			Assert.IsNull(feedback.ArchiveName);

			Assert.IsNotNull(message);
			Assert.That(message.Attachments.Length, Is.GreaterThan(0));
		}

		[Test]
		public void Get_message_without_attachments()
		{
			var feedback = new Feedback();
			trash.Add(feedback);
			var result = feedback.Send().GetEnumerator();
			Assert.IsFalse(result.MoveNext());
			var message = feedback.GetMessage();
			Assert.IsNull(feedback.ArchiveName);
			Assert.IsNull(message.Attachments);
		}
	}
}