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
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Inflector;
using NHibernate;
using NHibernate.Util;

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

		public override string ToString()
		{
			return "ResultDir: " + Name;
		}
	}


	public class Config : ICloneable
	{
		private log4net.ILog log = log4net.LogManager.GetLogger(typeof(Config));
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
		public string AltUri;
		public TimeSpan RequestInterval = TimeSpan.FromSeconds(15);
		public string RootDir;
		public string DiadokApiKey;
		public string DiadokUrl;
		public SimpleMRUCache Cache = new SimpleMRUCache(10);
		public bool MultiUser;

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

		public string UpdateTmpDir => Path.Combine(TmpDir, "update");
		public string BinUpdateDir => Path.Combine(UpdateTmpDir, "update");
		public string Opt => Path.Combine(RootDir, "Opt");

		public void InitDir()
		{
			Directory.CreateDirectory(RootDir);
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

		public string MapToFileProducerPromo(object entity)
		{
			try
			{
				var clazz = NHibernateUtil.GetClass(entity);
				var root = Path.Combine(RootDir, clazz.Name.Pluralize());
				root = root.Replace(@".\", @"\");
				var id = Util.GetValue(entity, "PromoFileId");
				return Directory.GetFiles(root, id + ".*").FirstOrDefault();
			}
			catch (DirectoryNotFoundException)
			{
				return null;
			}
		}

		public string SyncUrl(string key, DateTime? lastSync, IEnumerable<Address> addresses)
		{
			var queryString = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("reset", "true"),
				new KeyValuePair<string, string>("data", key)
			};

			if (lastSync != null)
				queryString.Add(new KeyValuePair<string, string>("lastSync", lastSync.Value.ToString("O")));
			if (addresses != null)
				queryString.Add(new KeyValuePair<string, string>("addressIds", String.Join(",", addresses.Select(x => x.Id))));

			return $"Main?{BuildQueryString(queryString)}";
		}

		private static string BuildQueryString(List<KeyValuePair<string, string>> queryString)
		{
			var stringBuilder = new StringBuilder();
			foreach (var keyValuePair in queryString) {
				if (stringBuilder.Length > 0)
					stringBuilder.Append('&');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Key));
				stringBuilder.Append('=');
				stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Value ?? ""));
			}
			return stringBuilder.ToString();
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
				new ResultDir("producerpromotions",settings, this)
			};
		}

		object ICloneable.Clone()
		{
			return MemberwiseClone();
		}

		public Config Clone()
		{
			return (Config)MemberwiseClone();
		}

		public BitmapImage LoadAd(string name)
		{
			//здесь может возникнуть ошибка при загрузке картинке например
			//System.NotSupportedException: No imaging component suitable to complete this operation was found.
			//---> System.Runtime.InteropServices.COMException: Exception from HRESULT: 0x88982F50
			//все бы ничего но caliburn деактивации\активации формы не сможет установить форму из-за этого исключения
			return Cache.Cache(name.ToLower(), x => {
				try {
					x = Path.Combine(RootDir, "ads", x);
					if (!File.Exists(x))
						return null;

					var bi = new BitmapImage();
					bi.BeginInit();
					bi.StreamSource = new MemoryStream(File.ReadAllBytes(x));
					bi.EndInit();
					bi.Freeze();
					return bi;
				} catch(Exception e) {
					log.Error($"Не удалось загрузить изображение {x}", e);
					return null;
				}
			});
		}
	}
}
