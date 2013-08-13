using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public enum DocType
	{
		Docs,
		Waybills,
		Rejects,
	}

	public class DocumentLog
	{
		public virtual uint Id { get; set; }
		public virtual uint? AddressId { get; set; }
		public virtual DocType DocumentType { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual string Filename { get; set; }
	}

	public class DocumentSendLog
	{
		public virtual uint Id { get; set; }
		public virtual uint UserId { get; set; }
		public virtual DocumentLog Document { get; set; }
		public virtual bool Committed { get; set; }
		public virtual bool WaitConfirm { get; set; }
		public virtual bool DocumentDelivered { get; set; }
		public virtual bool FileDelivered { get; set; }
	}
}