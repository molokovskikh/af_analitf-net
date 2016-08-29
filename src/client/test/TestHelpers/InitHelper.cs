using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using AnalitF.Net.Client.Test.Integration;
using AnalitF.Net.Service;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class InitHelper
	{
		public static Uri RandomPort()
		{
			return new Uri(String.Format("http://localhost:{0}", new Random(DateTime.Now.Millisecond).Next(10000, 20000)));
		}

		public static async Task<Tuple<HttpSelfHostServer, Service.Config.Config>> InitService(Uri url, Service.Config.Config config = null)
		{
			var cfg = new HttpSelfHostConfiguration(url) {
				IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always,
				HostNameComparisonMode = HostNameComparisonMode.Exact
			};
			config = Application.InitApp(cfg, config);
			config.UpdateLifeTime = TimeSpan.FromDays(1);
			var server = new HttpSelfHostServer(cfg);
			await server.OpenAsync();
			return Tuple.Create(server, config);
		}
	}
}