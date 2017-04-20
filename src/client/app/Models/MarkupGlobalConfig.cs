using System;
using System.ComponentModel;

namespace AnalitF.Net.Client.Models
{
	public class MarkupGlobalConfig : BaseStatelessObject
	{
		public override uint Id { get; set; }

		[Description("Тип")]
		public virtual MarkupType Type { get; set; }

		[Description("Левая граница цен")]
		public virtual decimal Begin { get; set; }

		[Description("Правая граница цен")]
		public virtual decimal End { get; set; }

		[Description("Наценка (%)")]
		public virtual decimal Markup { get; set; }

		[Description("Макс. наценка (%)")]
		public virtual decimal MaxMarkup { get; set; }

		[Description("Макс. наценка опт. звена (%)")]
		public virtual decimal MaxSupplierMarkup { get; set; }
	}
}