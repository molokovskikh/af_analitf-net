using System;
using AnalitF.Net.Client.Helpers;

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

		[Style(Description = "Предложения отсутствуют")]
		public virtual bool DoNotHaveOffers
		{
			get { return !HaveOffers; }
		}

		public virtual string FullName
		{
			get { return Name.Name + " " + Form; }
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}