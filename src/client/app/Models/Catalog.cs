using System;
using AnalitF.Net.Client.Helpers;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public class Catalog : BaseStatelessObject
	{
		public Catalog()
		{
		}

		public Catalog(string name)
		{
			Name = new CatalogName { Name = name };
			FullName = name;
		}

		public override uint Id { get; set; }

		public virtual uint FormId { get; set; }

		public virtual string Form { get; set; }

		public virtual CatalogName Name { get; set; }

		[Style(Description = "Жизненно важный")]
		public virtual bool VitallyImportant { get; set; }

		public virtual bool MandatoryList { get; set; }

		public virtual bool HaveOffers { get; set; }

		public virtual bool Hidden { get; set; }

		public virtual string FullName { get; set; }

		public virtual uint ProductId { get; set; }

		[Style(Description = "Предложения отсутствуют")]
		public virtual bool DoNotHaveOffers
		{
			get { return !HaveOffers; }
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}