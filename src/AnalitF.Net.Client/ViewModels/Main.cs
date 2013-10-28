using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Helpers;
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
			Readonly = true;
			Newses = new NotifyValue<List<News>>();
			Ad = new NotifyValue<string>();
		}

		public NotifyValue<List<News>> Newses { get; set; }
		public NotifyValue<string> Ad { get; set; }

		public override void Update()
		{
			Newses.Value = StatelessSession.Query<News>().OrderByDescending(n => n.PublicationDate).ToList();
			Newses.Value.Each(n => n.Init(Config));

			var filename = FileHelper.MakeRooted(@"ads\index.gif");
			if (File.Exists(filename))
				Ad.Value = filename;
		}
	}
}