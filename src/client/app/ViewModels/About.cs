using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class About : BaseScreen
	{
		public About()
		{
			DisplayName = "О программе";
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Version = typeof(ShellViewModel).Assembly.GetName().Version.ToString();
			AboutText = Addresses.Select(a => String.Format("адрес заказа {0}\n"
				+ "   E-mail для накладных: {1}@waybills.analit.net\n"
				+ "   E-mail для отказов: {1}@refused.analit.net", a.Name, a.Id))
				.Implode("\n\n");
		}

		public string AboutText { get; set; }
		public string Version { get; set; }
	}
}