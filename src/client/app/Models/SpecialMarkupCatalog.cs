using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Dapper;

namespace AnalitF.Net.Client.Models
{
	public class SpecialMarkupCatalog
	{

		public SpecialMarkupCatalog()
		{
		}

		public SpecialMarkupCatalog(CatalogDisplayItem value)
		{
			CatalogId = value.CatalogId;
			Name = value.Name;
			Form = value.Form;
		}

		public virtual uint Id { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual string Name { get; set; }

		public virtual string Form { get; set; }

		public static uint[] Load(IDbConnection connection)
		{
			return connection.Query<uint>(@"
select p.Id from SpecialMarkupCatalogs c
join Products p on p.CatalogId = c.CatalogId")
				.ToArray();
		}
	}
}