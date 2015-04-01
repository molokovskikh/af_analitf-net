namespace AnalitF.Net.Client.Models
{
	public class RegulatorRegistry
	{
		public virtual ulong Id { get; set; }
		public virtual string DrugID { get; set; }
		public virtual string InnR { get; set; }
		public virtual string TradeNmR { get; set; }
		public virtual string DrugFmNmRS { get; set; }
		public virtual string Pack { get; set; }
		public virtual string DosageR { get; set; }
		public virtual string ClNm { get; set; }
		public virtual string Segment { get; set; }
		public virtual uint ProductId { get; set; }
		public virtual uint ProducerId { get; set; }
	}
}