using System.IO;

namespace AnalitF.Net.Service.Models
{
	public class Attachment
	{
		public virtual uint Id { get; set; }
		public virtual string Filename { get; set; }
		public virtual string Extension { get; set; }
		public virtual uint MailId { get; set; }

		public virtual string GetFilename(Config.Config config)
		{
			return Path.Combine(config.AttachmentsPath, Id + Extension);
		}

		public virtual string GetArchiveName()
		{
			return "attachments/" + Id + Extension;
		}
	}
}