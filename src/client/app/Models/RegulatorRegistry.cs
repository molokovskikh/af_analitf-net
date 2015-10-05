namespace AnalitF.Net.Client.Models
{
	public class Drug
	{
		public virtual uint DrugId { get; set; }
		public virtual string TradeNmR { get; set; }
		public virtual string InnR { get; set; }
		public virtual string PackNx { get; set; }
		public virtual string DosageR { get; set; }
		public virtual string PackQn { get; set; }
		public virtual string Pack { get; set; }
		public virtual string DrugFmNmRS { get; set; }
		public virtual string Segment { get; set; }
		public virtual string Year { get; set; }
		public virtual string Month { get; set; }
		public virtual string Series { get; set; }
		public virtual string TotDrugQn { get; set; }
		public virtual string MnfPrice { get; set; }
		public virtual string PrcPrice { get; set; }
		public virtual string RtlPrice { get; set; }
		public virtual string Funds { get; set; }
		public virtual string VendorID { get; set; }
		public virtual string Remark { get; set; }
		public virtual string SrcOrg { get; set; }
		public virtual string EAN { get; set; }
		public virtual string MaxMnfPrice { get; set; }
		public virtual string ExpiTermR { get; set; }
		public virtual string ClNm { get; set; }
		public virtual string MnfNm { get; set; }
		public virtual string PckNm { get; set; }
		public virtual string RegNr { get; set; }
		public virtual string RegDate { get; set; }
	}

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