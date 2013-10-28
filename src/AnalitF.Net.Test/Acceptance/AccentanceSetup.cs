using System.IO;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[SetUpFixture]
	public class AccentanceSetup
	{
		[SetUp]
		public void Setup()
		{
			Prepare(@"..\..\..\AnalitF.Net.Client\bin\Debug", "acceptance");
		}

		protected static void Prepare(string src, string dst)
		{
			if (!Directory.Exists(dst))
				Directory.CreateDirectory(dst);

			var files = Directory.GetFiles(src, "*.exe")
				.Concat(Directory.GetFiles(src, "*.dll"))
				.Concat(Directory.GetFiles(src, "*.config"));
			files.Each(f => File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true));

			FileHelper2.CopyDir("share", Path.Combine(dst, "share"));
			FileHelper2.CopyDir("backup", Path.Combine(dst, "data"));
		}
	}
}