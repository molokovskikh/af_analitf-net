using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Xml.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.Integration;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NUnit.Framework;
using Test.Support;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[SetUpFixture]
	public class AccentanceSetup
	{
		public static IntegrationSetup integrationSetup;

		[SetUp]
		public void Setup()
		{
			if (!Directory.Exists(IntegrationSetup.BackupDir))
				return;
			Prepare(@"..\..\..\app\bin\Debug", "acceptance");

			var port = Generator.Random(ushort.MaxValue).First();
			var url = String.Format("http://localhost:{0}/", port);

			integrationSetup = new IntegrationSetup();
			integrationSetup.InitWebServer(new Uri(url)).Wait();

			Configure("acceptance", url);
		}

		[TearDown]
		public void Teardown()
		{
			if (integrationSetup != null && integrationSetup.server != null)
				integrationSetup.server.Dispose();
		}

		public static void Configure(string binDir, string uri)
		{
			UpdateChannel(Path.Combine(binDir, "analitf.net.client.exe.config"), uri);
		}

		private static void UpdateChannel(string config, string url)
		{
			var doc = XDocument.Load(config);
			var node = doc.Descendants().Where(n => n.Name == "appSettings")
				.SelectMany(n => n.Descendants().Where(x => x.Name.LocalName == "add" && (string)x.Attribute("key") == "Uri"))
				.First();
			node.Attribute("value").Value = url;
			doc.Save(config);
		}

		protected static void Prepare(string src, string dst)
		{
			Directory.CreateDirectory(dst);
			DataHelper.CopyBin(src, dst);
			FileHelper2.CopyDir("share", Path.Combine(dst, "share"));
			FileHelper2.CopyDir(IntegrationSetup.BackupDir, Path.Combine(dst, "data"));
		}
	}
}