namespace AnalitF.Net.Client.Models
{
	public enum DocumentType
	{
		Docs,
		Waybills,
		Rejects,
	}

	public class LoadedDocument
	{
		public virtual uint Id { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual DocumentType Type { get; set; }
		public virtual string OriginFilename { get; set; }
	}
}