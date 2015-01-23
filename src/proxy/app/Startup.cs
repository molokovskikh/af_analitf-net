using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Tools;
using log4net;
using log4net.Config;
using Microsoft.Owin;
using Owin;

namespace AnalitF.Net.Proxy
{
	public class Startup
	{
		private static ILog log = LogManager.GetLogger(typeof(Startup));

		public void Configuration(IAppBuilder app)
		{
			try {
				XmlConfigurator.Configure();
				var configfile = Path.GetFullPath(FileHelper.MakeRooted(ConfigurationManager.AppSettings["config"] ?? "config"));
				LoadConfig(configfile);
				var watched = new FileSystemWatcher(Path.GetDirectoryName(configfile), Path.GetFileName(configfile));
				watched.Changed += (sender, args) => LoadConfig(configfile);
				watched.Deleted += (sender, args) => LoadConfig(configfile);
				watched.Created += (sender, args) => LoadConfig(configfile);
				watched.Renamed += (sender, args) => LoadConfig(configfile);
				watched.Error += (sender, args) => log.Error("watch error", args.GetException());
				watched.EnableRaisingEvents = true;
				app.Use(new Func<object, Func<IDictionary<string, object>, Task>>(_ => Proxy.Invoke));
			}
			catch(Exception e) {
				log.Error(e);
			}
		}

		private static void LoadConfig(string configfile)
		{
			if (File.Exists(configfile)) {
				var lines = File.ReadAllLines(configfile);
				var route = lines.FirstOrDefault();
				if (route != null)
					Proxy.DefaultRoute = route;

				Proxy.Map = lines.Skip(1)
					.Select(l => l.Split(' '))
					.Where(l => l.Length == 2)
					.Select(l => Tuple.Create(new Version(l[0]), l[1]))
					.GroupBy(t => t.Item1)
					.ToDictionary(g => g.Key, g => g.Select(t => t.Item2).First());
				log.Debug(Proxy.DefaultRoute);
				log.Debug(Proxy.Map.Implode(Environment.NewLine));
			}
			else {
				log.DebugFormat("config {0} not found", configfile);
			}
		}
	}

	public class Proxy
	{
		private static ILog log = LogManager.GetLogger(typeof(Proxy));

		public static Dictionary<Version, string> Map = new Dictionary<Version, string>();
		public static string DefaultRoute;

		public async static Task Invoke(IDictionary<string, object> environment)
		{
			try {
				var context = (IOwinContext) new OwinContext(environment);
				var route = DefaultRoute;
				Version version;
				log.DebugFormat("{0} {1}", context.Request.Method, context.Request.Uri);
				foreach (var header in context.Request.Headers) {
					log.DebugFormat("{0}: {1}", header.Key, header.Value.Implode());
				}

				if (Version.TryParse(context.Request.Headers["Version"], out version)) {
					route = Map.GetValueOrDefault(version) ?? DefaultRoute;
				}
				if (route == null) {
					log.Error("route not found");
					context.Response.StatusCode = 500;
				}
				else {
					var contentHeaders = new HashSet<string>();
					contentHeaders.Add("Allow");
					contentHeaders.Add("Content-Disposition");
					contentHeaders.Add("Content-Encoding");
					contentHeaders.Add("Content-Language");
					contentHeaders.Add("Content-Length");
					contentHeaders.Add("Content-Location");
					contentHeaders.Add("Content-MD5");
					contentHeaders.Add("Content-Range");
					contentHeaders.Add("Content-Type");
					contentHeaders.Add("Expires");
					contentHeaders.Add("Last-Modified");

					var uri = route + context.Request.Path;
					var querystring = (string)environment["owin.RequestQueryString"];
					if (querystring != "")
						uri += "?" + querystring;

					var client = new HttpClient();
					var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);
					log.DebugFormat("request = {0}", proxyRequest);
					proxyRequest.Headers.Clear();
					proxyRequest.Headers.Add("X-Forwarded-For", context.Request.RemoteIpAddress);
					foreach (var header in context.Request.Headers.Where(h => !contentHeaders.Contains(h.Key))) {
						proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
					}
					if (context.Request.Body != null && proxyRequest.Method != HttpMethod.Get) {
						proxyRequest.Content = new StreamContent(context.Request.Body);
						var bodyHeaders = context.Request.Headers.Where(h => contentHeaders.Contains(h.Key));
						foreach (var header in bodyHeaders) {
							proxyRequest.Content.Headers.Add(header.Key, header.Value);
						}
					}
					log.DebugFormat("send {0}", proxyRequest);
					var proxyResponse = await client.SendAsync(proxyRequest);
					log.DebugFormat("got {0}", proxyResponse);

					context.Response.StatusCode = (int)proxyResponse.StatusCode;
					context.Response.Headers.Keys
						.ToArray()
						.Each(k => context.Response.Headers.Remove(k));

					foreach (var header in proxyResponse.Headers) {
						if (header.Key == "Transfer-Encoding")
							continue;
						context.Response.Headers.Add(header.Key, header.Value.ToArray());
					}
					if (proxyResponse.Content != null) {
						foreach (var header in proxyResponse.Content.Headers) {
							if (context.Response.Headers.ContainsKey(header.Key))
								context.Response.Headers.SetValues(header.Key, header.Value.ToArray());
							else
								context.Response.Headers.Add(header.Key, header.Value.ToArray());
						}
						await proxyResponse.Content.CopyToAsync(context.Response.Body);
					}
				}
			}
			catch (Exception e) {
				log.Error(e);
				throw;
			}
		}
	}
}