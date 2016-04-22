using System;
using System.Collections.Generic;
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

		public virtual bool VitallyImportant { get; set; }

		public virtual bool MandatoryList { get; set; }

		public virtual bool HaveOffers { get; set; }

		//в отображении не участвует, используется для очистки удаленных записей
		public virtual bool Hidden { get; set; }

		public virtual string FullName { get; set; }

		public virtual bool Narcotic { get; set; }

		public virtual bool Toxic { get; set; }

		public virtual bool Combined { get; set; }

		public virtual bool Other { get; set; }

		public virtual bool IsPKU => Narcotic || Toxic || Combined || Other;

		public virtual string PKU
		{
			get
			{
				if (Narcotic)
					return "ПКУ:Наркотические и психотропные";
				if (Toxic)
					return "ПКУ:Сильнодействующие. и ядовитые";
				if (Combined)
					return "ПКУ:Комбинированные";
				if (Other)
					return "ПКУ:Иные лек.средства";
				return null;
			}
		}

		[Style(Description = "Предложения отсутствуют")]
		public virtual bool DoNotHaveOffers => !HaveOffers;

		public override string ToString()
		{
			return FullName;
		}
	}
}