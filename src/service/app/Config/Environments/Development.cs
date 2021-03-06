﻿using System;
using System.IO;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Service.Config.Environments
{
	public class Development
	{
		public string BasePath;

		public Development()
		{
			BasePath = AppDomain.CurrentDomain.BaseDirectory;
		}

		public void Run(Config config)
		{
			var properties = config.GetType().GetProperties()
				.Where(p => p.CanWrite)
				.Where(p => p.Name.EndsWith("Path") && p.PropertyType == typeof(string))
				.Where(p => p.Name != "RemoteExportPath");

			foreach (var property in properties) {
				var value = (string)property.GetValue(config, null);
				if (string.IsNullOrEmpty(value)) {
					value = property.Name.ToLower().Replace("path", "");
					value = Path.Combine(config.RootPath, value);
				}

				if (!Path.IsPathRooted(value))
					value = Path.Combine(BasePath, value);

				Directory.CreateDirectory(value);
				property.SetValue(config, value, null);
			}

			config.RemoteExportPath = config.LocalExportPath;
		}
	}
}