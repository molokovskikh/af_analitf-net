using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Documents;

namespace AnalitF.Net.Client.Models
{
	public class ProductDescription
	{
		public virtual uint Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string EnglishName { get; set; }

		[Display(Name = "Состав:", Order = 0)]
		public virtual string Composition { get; set; }

		[Display(Name = "Фармакологическое действие:", Order = 1)]
		public virtual string PharmacologicalAction { get; set; }

		[Display(Name = "Показания к применению:", Order = 2)]
		public virtual string IndicationsForUse { get; set; }

		[Display(Name = "Способ применения и дозы:", Order = 3)]
		public virtual string Dosing { get; set; }

		[Display(Name = "Предостережения и противопоказания:", Order = 4)]
		public virtual string Warnings { get; set; }

		[Display(Name = "Побочные действия:", Order = 5)]
		public virtual string SideEffect { get; set; }

		[Display(Name = "Взаимодействие:", Order = 6)]
		public virtual string Interaction { get; set; }

		[Display(Name = "Форма выпуска:", Order = 7)]
		public virtual string ProductForm { get; set; }

		[Display(Name = "Дополнительно:", Order = 8)]
		public virtual string Description { get; set; }

		[Display(Name = "Условия хранения:", Order = 8)]
		public virtual string Storage { get; set; }

		[Display(Name = "Срок годности:", Order = 8)]
		public virtual string Expiration { get; set; }

		public virtual string FullName
		{
			get { return String.Format("{0} ({1})", Name, EnglishName); }
		}
	}
}