﻿using System.ComponentModel;

namespace AnalitF.Net.Client.Models
{
	public class Mnn : BaseStatelessObject
	{
		public override uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual bool HaveOffers { get; set; }

		[Style(Description = "Предложения отсутствуют")]
		public virtual bool DoNotHaveOffers
		{
			get { return !HaveOffers; }
		}

		public override string ToString()
		{
			return Name;
		}
	}
}