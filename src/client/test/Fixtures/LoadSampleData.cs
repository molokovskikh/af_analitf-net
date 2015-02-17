using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Service.Models;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LoadSampleData
	{
		public List<UpdateData> Files;
		public Config.Config Config;

		public LoadSampleData(List<UpdateData> files)
		{
			Files = files;
		}

		public void Execute(ISession session)
		{
			var sanityCheck = new SanityCheck();
			sanityCheck.Config = Config;
			sanityCheck.InitDb();

			var result = Files.GroupBy(f => f.ArchiveFileName.Replace(".meta", ""))
				.Where(g => g.Count() > 1)
				.Select(g => Tuple.Create(
					g.First(f => f.ArchiveFileName.IndexOf(".meta.") == -1).LocalFileName,
					g.First(f => f.ArchiveFileName.IndexOf(".meta.") >= 0)
						.ReadContent()
						.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)))
				.ToList();

			var importer = new ImportCommand(result);
			importer.Config = Config;
			importer.Session = session;
			importer.Execute();

			var settings = session.Query<Settings>().First();
			settings.UserName = "test";
			settings.Password = "123";
			session.Save(settings);
		}
	}
}