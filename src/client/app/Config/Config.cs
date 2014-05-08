using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common.MySql;
using Iesi.Collections;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Inflector;
using NHibernate;

namespace AnalitF.Net.Client.Config
{
	public class Config
	{
		private string dbDir;
		private string tmpDir;
		private string settingsPath;

		public Config()
		{
			RootDir = ".";
			TmpDir = "temp";
			DbDir = "data";
			SettingsPath = GetType().Assembly.GetName().Name + ".data";
		}

		public string DebugPipeName;
		public bool Quiet;
		public string Cmd;
		public Uri BaseUrl;
		public TimeSpan RequestInterval = TimeSpan.FromSeconds(15);
		public string RootDir;

		public string TmpDir
		{
			get
			{
				if (Path.IsPathRooted(tmpDir))
					return tmpDir;
				return Path.Combine(RootDir, tmpDir);
			}
			set { tmpDir = value; }
		}

		public string DbDir
		{
			get
			{
				if (Path.IsPathRooted(dbDir))
					return dbDir;
				return Path.Combine(RootDir, dbDir);
			}
			set { dbDir = value; }
		}

		public string SettingsPath
		{
			get
			{
				if (Path.IsPathRooted(settingsPath))
					return settingsPath;
				return Path.Combine(RootDir, settingsPath);
			}
			set { settingsPath = value; }
		}

		public string ArchiveFile
		{
			get { return Path.Combine(TmpDir, "archive.zip"); }
		}

		public string UpdateTmpDir
		{
			get { return Path.Combine(TmpDir, "update");}
		}

		public string BinUpdateDir
		{
			get { return Path.Combine(UpdateTmpDir, "update"); }
		}

		public void InitDir()
		{
			if (!Directory.Exists(RootDir))
				Directory.CreateDirectory(RootDir);
			if (!Directory.Exists(TmpDir))
				Directory.CreateDirectory(TmpDir);
		}

		public string MapToFile(object entity)
		{
			try
			{
				var clazz = NHibernateUtil.GetClass(entity);
				var root = Path.Combine(RootDir, clazz.Name.Pluralize());
				var id = Util.GetValue(entity, "Id");
				return Directory.GetFiles(root, id + ".*").FirstOrDefault();
			}
			catch(DirectoryNotFoundException) {
				return null;
			}
		}

		public Uri WaitUrl(Uri url)
		{
			var builder = new UriBuilder(url) {
				Query = ""
			};
			return builder.Uri;
		}

		public Uri SyncUrl(string key)
		{
			var queryString = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("reset", "true"),
				new KeyValuePair<string, string>("data", key)
			};

			var stringBuilder = new StringBuilder();
			foreach (var keyValuePair in queryString) {
				if (stringBuilder.Length > 0)
					stringBuilder.Append('&');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Key));
				stringBuilder.Append('=');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Value));
			}
			var builder = new UriBuilder(new Uri(BaseUrl, "Main")) {
				Query = stringBuilder.ToString(),
			};
			var url = builder.Uri;
			return url;
		}
	}
}
