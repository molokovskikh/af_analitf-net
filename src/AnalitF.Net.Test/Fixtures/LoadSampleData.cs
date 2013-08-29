using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Service.Models;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LoadSampleData
	{
		public bool Local = false;
		public TestClient Client;
		public TestPrice MaxProducerCosts;

		public void Execute(ISession session)
		{
			new SanityCheck("").InitDb();

			var exporter = new Exporter(session, Client.Users[0].Id, new Version()) {
				MaxProducerCostPriceId = MaxProducerCosts.Id,
				MaxProducerCostCostId = MaxProducerCosts.Costs[0].Id
			};
			var files = new List<UpdateData>();
			exporter.Export(files);

			var result = files.GroupBy(f => f.ArchiveFileName.Replace(".meta", ""))
				.Where(g => g.Count() > 1)
				.Select(g => Tuple.Create(g.First(f => f.Content == null).LocalFileName,
					g.First(f => f.Content != null).Content.Split(new[] { "\r\n" },
						StringSplitOptions.RemoveEmptyEntries)))
				.ToList();

			var importer = new Importer(session);
			importer.Import(result, new ProgressReporter());

			var settings = session.Query<Settings>().First();
			settings.UserName = "test";
			settings.Password = "123";
			session.Save(settings);
		}
	}
}