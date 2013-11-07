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
		public bool Local = true;
		public List<UpdateData> Files;
		public Config.Config Config;

		public void Execute(ISession session)
		{
			new SanityCheck("").InitDb();
			var result = Files.GroupBy(f => f.ArchiveFileName.Replace(".meta", ""))
				.Where(g => g.Count() > 1)
				.Select(g => Tuple.Create(g.First(f => f.Content == null).LocalFileName,
					g.First(f => f.Content != null).Content.Split(new[] { "\r\n" },
						StringSplitOptions.RemoveEmptyEntries)))
				.ToList();

			var importer = new ImportCommand(result);
			importer.Session = session;
			importer.Execute();

			var settings = session.Query<Settings>().First();
			settings.UserName = "test";
			settings.Password = "123";
			session.Save(settings);
		}
	}
}