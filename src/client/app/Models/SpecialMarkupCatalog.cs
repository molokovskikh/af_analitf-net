using System;
using System.Collections.Generic;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public class SpecialMarkupCatalog
	{
		public virtual uint Id { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual uint ProductId { get; set; }
	}
}