using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class Main : BaseScreen
	{
		public Config.Config Config;

		public Main(Config.Config config)
		{
			Config = config;
			DisplayName = "";
			Newses = new List<News>();
			Readonly = true;
		}

		public List<News> Newses { get; set; }
		public string Ad { get; set; }

		public override void Update()
		{
			Newses = StatelessSession.Query<News>().OrderByDescending(n => n.PublicationDate).ToList();
			Newses.Each(n => n.Init(Config));

			var filename = FileHelper.MakeRooted(@"ads\index.gif");
			if (File.Exists(filename))
				Ad = filename;
		}
	}
}