using System;
using System.IO;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Service.Config.Environments
{
	public class Production
	{
		public void Run(Config config)
		{
			if (!Path.IsPathRooted(config.RemoteExportPath))
				config.RemoteExportPath = @"\\" + Environment.MachineName + @"\" + config.RemoteExportPath;

			var properties = config.GetType().GetProperties()
				.Where(p => p.CanWrite)
				.Where(p => p.Name.EndsWith("Path") && p.PropertyType == typeof(string));

			foreach (var property in properties) {
				var value = (string)property.GetValue(config, null);
				if (value != null && !Path.IsPathRooted(value))
					value = FileHelper.MakeRooted(value);

				property.SetValue(config, value, null);
			}

			if (!Directory.Exists(config.CachePath))
				Directory.CreateDirectory(config.CachePath);
			if (!Directory.Exists(config.ResultPath))
				Directory.CreateDirectory(config.ResultPath);
		}
	}
}