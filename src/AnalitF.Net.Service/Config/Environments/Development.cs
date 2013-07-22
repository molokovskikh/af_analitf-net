using System.IO;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Service.Config.Environments
{
	public class Development
	{
		public void Run(Config config)
		{
			var properties = config.GetType().GetProperties()
				.Where(p => p.Name.EndsWith("Path") && p.PropertyType == typeof(string));

			foreach (var property in properties) {
				var value = (string)property.GetValue(config, null);
				if (string.IsNullOrEmpty(value)) {
					value = property.Name.ToLower().Replace("path", "");
					value = Path.Combine(config.RootPath, value);
				}

				if (!Path.IsPathRooted(value))
					value = FileHelper.MakeRooted(value);

				FileHelper.CreateDirectoryRecursive(value);
				property.SetValue(config, value, null);
			}
		}
	}
}