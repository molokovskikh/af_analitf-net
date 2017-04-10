using System;
//using System.Collections.Generic;
using AnalitF.Net.Client.Helpers;


namespace AnalitF.Net.Client.Models
{

  public	class Category : BaseStatelessObject
	{
		public override uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual uint GroupId { get; set; }
	}

}
