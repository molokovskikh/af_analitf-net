using System;
using System.IO;
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
		public virtual bool IsFake { get; set; }
		public virtual bool PreserveFilename { get; set; }
		public virtual DateTime LogTime { get; set; }
	}

	public class DocumentSendLog
	{
		public virtual uint Id { get; set; }
		public virtual User User { get; set; }
		public virtual DocumentLog Document { get; set; }
		public virtual bool Committed { get; set; }
		public virtual bool DocumentDelivered { get; set; }
		public virtual bool FileDelivered { get; set; }
		public virtual DateTime? SendDate { get; set; }

		public virtual string GetTargetFilename(string localFilename)
		{
			var filename = Path.GetFileName(localFilename);
			if (Document.PreserveFilename)
				filename = Document.Filename ?? filename;
			return filename;
		}
	}
}