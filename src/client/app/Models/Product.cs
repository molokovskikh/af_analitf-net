using System;
using System.Collections.Generic;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public class Product
	{
		public virtual uint Id { get; set; }

		public virtual uint CatalogId { get; set; }
	}
}