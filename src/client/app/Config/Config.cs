using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.MySql;
using Common.Tools;
using Iesi.Collections;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Inflector;
using NHibernate;

namespace AnalitF.Net.Client.Config
{
	public class ResultDir
	{
		private static string[] autoOpenFiles = {
			"attachments"
		};

		private static string[] autoOpenDirs = {
			"waybills", "rejects", "docs"
		};

		public ResultDir(string name, Settings settings, Config confg)
		{
			var openTypes = autoOpenFiles.ToList();
			if (settings.OpenRejects) {
				openTypes.Add("rejects");
			}
			if (settings.OpenWaybills) {
				openTypes.Add("waybills");
			}
			Name = name;
			Src = Path.Combine(confg.UpdateTmpDir, Name);
			Dst = settings.MapPath(name) ?? Path.Combine(confg.RootDir, name);
			if (name.Match("waybills")) {
				GroupBySupplier = settings.GroupWaybillsBySupplier;
			}
			if (name.Match("ads")) {
				Clean = true;
			}
			OpenFiles = openTypes.Any(t => t.Match(name));
			OpenDir = autoOpenDirs.Any(d => d.Match(name));
		}

		public string Name;
		public string Src;
		public string Dst;
		public bool OpenFiles;
		public bool OpenDir;
		public bool GroupBySupplier;
		/// <summary>
		/// Очистить директорию перед перемещением файлов
		/// </summary>
		public bool Clean;
		public IList<string> ResultFiles = new List<string>();

		public static IEnumerable<IResult> OpenResultFiles(IEnumerable<ResultDir> dirs)
		{
			var disableOpenFiles = dirs.Where(d => d.OpenFiles).Sum(g => g.ResultFiles.Count) > 5;
			if (disableOpenFiles) {
				dirs.Each(d => d.OpenFiles = false);
			}

			foreach (var dir in dirs.Where(d => d.ResultFiles.Count > 0)) {
				if (dir.OpenFiles) {
					foreach (var f in dir.ResultFiles.Select(f => new OpenResult(f))) {
						yield return f;
					}
				}
				else if (dir.OpenDir) {
					yield return new OpenResult(dir.Dst);
				}
			}
		}
	}


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
#if DEBUG
		public bool IsUnitTesting;
		public bool SkipOpenSession;
#endif

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

		public Uri SyncUrl(string key, DateTime? lastSync)
		{
			var queryString = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("reset", "true"),
				new KeyValuePair<string, string>("data", key)
			};

			if (lastSync != null)
				queryString.Add(new KeyValuePair<string, string>("lastSync", lastSync.Value.ToString("O")));

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
			return builder.Uri;
		}

		public List<ResultDir> KnownDirs(Settings settings)
		{
			return new List<ResultDir> {
				new ResultDir("ads", settings, this),
				new ResultDir("newses", settings, this),
				new ResultDir("waybills", settings, this),
				new ResultDir("docs", settings, this),
				new ResultDir("rejects", settings, this),
				new ResultDir("attachments", settings, this),
				new ResultDir("promotions", settings, this),
			};
		}
	}
}
