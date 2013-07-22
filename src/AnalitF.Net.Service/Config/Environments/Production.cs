using System.IO;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Service.Config.Environments
{
	public class Production
	{
		public void Run(Config config)
		{
			var properties = config.GetType().GetProperties()
				.Where(p => p.Name.EndsWith("Path") && p.PropertyType == typeof(string));

			foreach (var property in properties) {
				var value = (string)property.GetValue(config, null);
				if (!Path.IsPathRooted(value))
					value = FileHelper.MakeRooted(value);

				property.SetValue(config, value, null);
			}
		}
	}
}