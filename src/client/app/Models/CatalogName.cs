using System;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public class CatalogName : BaseStatelessObject
	{
		public override uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual bool HaveOffers { get; set; }

		[Style(Description = "Жизненно важный")]
		public virtual bool VitallyImportant { get; set; }

		public virtual bool MandatoryList { get; set; }

		public virtual Mnn Mnn { get; set; }

		public virtual ProductDescription Description { get; set; }

		//в отображении не участвует, используется для очистки удаленных записей
		public virtual bool Hidden { get; set; }

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

		//на форме список препаратов нужно отображать
		//наименование или наименование + форму в зависимости
		//от того какая таблица актина
		//поле нужно что бы работал биндинг CurrentItem_FullName
		public virtual string FullName => Name;

		public override string ToString()
		{
			return Name;
		}
	}
}