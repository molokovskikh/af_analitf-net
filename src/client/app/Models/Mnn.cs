using System;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public class Mnn : BaseStatelessObject
	{
		public override uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual bool HaveOffers { get; set; }

		public virtual bool Hidden { get; set; }

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