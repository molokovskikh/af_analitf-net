using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class AnalitfNetData
	{
		public AnalitfNetData()
		{
		}

		public AnalitfNetData(User user)
		{
			User = user;
			LastPendingUpdateAt = DateTime.Now;
		}

		public virtual uint Id { get; set; }
		public virtual User User { get; set; }
		public virtual DateTime LastUpdateAt { get; set; }
		public virtual DateTime? LastPendingUpdateAt { get; set; }
		public virtual string BinUpdateChannel { get; set; }
	}
}