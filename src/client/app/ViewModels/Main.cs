using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class Main : BaseScreen
	{
		public Main()
		{
			DisplayName = "Новости";
			InitFields();
		}

		public NotifyValue<User> CurrentUser { get; set; }
		public NotifyValue<List<News>> Newses { get; set; }
		public NotifyValue<BitmapImage> Ad { get; set; }

		public override void Update()
		{
			RxQuery(s => s.Query<User>().FetchMany(x => x.Permissions).FirstOrDefault())
				.Subscribe(x => {
					CurrentUser.Value = x ?? new User {
						SupportHours = "будни: с 07:00 до 19:00",
						SupportPhone = "тел.: 473-260-60-00",
					};
				});
			RxQuery(s => s.Query<News>().OrderByDescending(n => n.PublicationDate).ToList())
				.Subscribe(x => {
					x.Each(y => y.Init(Shell.Config));
					Newses.Value = x;
				});

			Ad.Value = Shell.Config.LoadAd("index.gif");
		}
	}
}