using System;
using System.IO;
using System.Linq;
using System.Web.Http.SelfHost;
using System.Xml.Linq;
using AnalitF.Net.Client.Test.Integration;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[SetUpFixture]
	public class AccentanceSetup
	{
		private static HttpSelfHostServer server;
		public static Service.Config.Config Config;
		public static Uri Url;

		[OneTimeSetUp]
		public void Setup()
		{
			Assert.IsNull(server);
			if (!Directory.Exists(IntegrationSetup.BackupDir))
				return;
			Prepare(@"..\..\..\app\bin\Debug", "acceptance");

			Url = InitHelper.RandomPort();
			var result = InitHelper.InitService(Url).Result;
			server = result.Item1;
			Config = result.Item2;
			Configure("acceptance", Url.ToString());
		}

		[OneTimeTearDown]
		public void Teardown()
		{
			server?.Dispose();
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
			DbHelper.CopyBin(src, dst);
			FileHelper.CopyDir("share", Path.Combine(dst, "share"));
			FileHelper.CopyDir(IntegrationSetup.BackupDir, Path.Combine(dst, "data"));
		}
	}
}