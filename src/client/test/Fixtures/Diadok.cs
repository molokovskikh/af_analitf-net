using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto.Events;
using NHibernate;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class Diadok
	{
		public void Execute(ISession session)
		{
			DiadocApi api = new DiadocApi(/*ConfigurationManager.AppSettings["DiadokApi"]*/"Analit-988b9e85-1b8e-40a9-b6bd-543790d0a7ec",
				"https://diadoc-api.kontur.ru", new WinApiCrypt());
			var token = api.Authenticate("133@analit.net", "A123456");
			var msg = new MessageToPost();
			msg.FromBoxId = "99f634490f4e469da56dd47b724eba81@diadoc.ru";
			msg.ToBoxId = "a3b9e01bddef496fa4bf60351a7c188b@diadoc.ru";
			var filename = Glob.Glob.Expand(@"C:\Users\kvasov\tmp\diadok\**\*.cs").OrderBy(_ => Guid.NewGuid()).First();
			msg.NonformalizedDocuments.Add(new NonformalizedAttachment {
				SignedContent = new SignedContent {
					Content = File.ReadAllBytes(filename.FullName),
					SignWithTestSignature = true,
				},
				FileName = Path.GetFileName(filename.FullName)
			});
			var m = api.PostMessage(token, msg);
			Console.WriteLine($"{m.MessageId} - {filename}");
		}
	}
}