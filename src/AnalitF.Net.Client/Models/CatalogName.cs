namespace AnalitF.Net.Client.Models
{
	public class CatalogName
	{
		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual bool HaveOffers { get; set; }

		public virtual bool VitallyImportant { get; set; }

		public virtual bool MandatoryList { get; set; }

		public virtual Mnn Mnn { get; set; }

		public virtual ProductDescription Description { get; set; }

		//на форме список препаратов нужно отображать
		//наименование или наименование + форму в зависимости
		//от того какая таблица актина
		//поле нужно что бы работал биндинг CurrentItem_FullName
		public virtual string FullName
		{
			get { return Name; }
		}

		public override string ToString()
		{
			return Name;
		}
	}
}