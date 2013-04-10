using AnalitF.Net.Client.Models;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Helpers
{
	public class JsonHelper
	{
		public static JsonSerializerSettings SerializerSettings()
		{
			var settings = new JsonSerializerSettings {
				ContractResolver = new NHibernateResolver()
			};
			return settings;
		}
	}
}