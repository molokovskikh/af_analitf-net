using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class ClientAppLog
	{
		public ClientAppLog()
		{
		}

		public ClientAppLog(User user, string text)
		{
			CreatedOn  = DateTime.Now;
			User = user;
			Text = text;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime CreatedOn { get; set; }
		public virtual User User { get; set; }
		public virtual string Text { get; set; }
	}
}