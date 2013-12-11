using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
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