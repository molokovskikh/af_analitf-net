using System;
using System.IO;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class AttachmentFixture
	{
		[TearDown]
		public void Teardown()
		{
			File.Delete("1.txt");
		}

		[Test]
		public void Load_icon()
		{
			File.WriteAllText("1.txt", "");
			var attachment = new Attachment();
			attachment.LocalFilename = "1.txt";
			Assert.IsNotNull(attachment.FileTypeIcon);
		}
	}
}