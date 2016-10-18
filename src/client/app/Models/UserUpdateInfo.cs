using System;

namespace AnalitF.Net.Client.Models
{
	public class UserUpdateInfo
	{
		public virtual uint Id { get; set; }

		public virtual uint UserId { get; set; }

		public virtual DateTime? UpdateDate { get; set; }

		public virtual DateTime? ReclameDate { get; set; }

		public virtual DateTime? UncommitedUpdateDate { get; set; }

		public virtual DateTime? UncommitedReclameDate { get; set; }

		public virtual string AFCopyId { get; set; }

		public virtual uint AFAppVersion { get; set; }

		public virtual string Message { get; set; }

		public virtual uint MessageShowCount { get; set; }

		public virtual uint SaveAFDataFiles { get; set; }

		public virtual uint InstallNet { get; set; }
	}
}
