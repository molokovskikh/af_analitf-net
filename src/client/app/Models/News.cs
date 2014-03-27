using System;
using System.IO;

namespace AnalitF.Net.Client.Models
{
	public class News
	{
		private Config.Config config;

		public virtual uint Id { get; set; }

		public virtual DateTime PublicationDate { get; set; }

		public virtual string Header { get; set; }

		public virtual Uri Url
		{
			get
			{
				if (config == null)
					return null;
				var path = Path.GetFullPath(Path.Combine(config.RootDir, "newses", Id + ".html"));

				return new Uri(path);
			}
		}

		//что бы nhibernate не ныл
		public virtual void Init(Config.Config config)
		{
			this.config = config;
		}
	}
}