﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class CheckLine
	{
		public virtual uint Id { get; set; }
		public virtual uint Barcode { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual string ProductName { get; set; }
		public virtual decimal RetailCost { get; set; }
		public virtual decimal Cost { get; set; }
		public virtual decimal Quantity { get; set; }
		public virtual decimal RetailSum => Quantity * Cost;
		public virtual decimal Sum => RetailSum - DiscontSum;
		public virtual decimal DiscontSum  { get; set; }
		public virtual uint CheckId { get; set; }
	}
}
