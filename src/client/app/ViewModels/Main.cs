using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
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
			InitFields();
		}

		public NotifyValue<List<News>> Newses { get; set; }
		public NotifyValue<BitmapImage> Ad { get; set; }
		public NotifyValue<string> SupportPhone { get; set; }
		public NotifyValue<string> SupportHours { get; set; }

		public override void Update()
		{
			Session.Evict(User);
			User = Session.Query<User>().FirstOrDefault()
				?? new User {
					SupportHours = "будни: с 07:00 до 19:00",
					SupportPhone = "тел.: 473-260-60-00",
				};
			Newses.Value = StatelessSession.Query<News>().OrderByDescending(n => n.PublicationDate).ToList();
			Newses.Value.Each(n => n.Init(Config));
			Ad.Value = Config.LoadAd("index.gif");
			SupportPhone.Value = User.SupportPhone;
			SupportHours.Value = User.SupportHours;
		}
	}
}