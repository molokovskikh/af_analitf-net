using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.Models
{
	public class BarcodeProducts : BaseStatelessObject
	{
		public BarcodeProducts()
		{
		}

		public BarcodeProducts(Product product, Producer producer, string barcode)
		{
			Product = product;
			Producer = producer;
			Barcode = barcode;
		}

		public override uint Id { get; set; }
		public virtual Product Product { get; set; }
		public virtual Producer Producer { get; set; }
		public virtual string Barcode { get; set; }
	}
}
