using System;
using System.IO;

namespace AnalitF.Net.Client.Config
{
	public class Config
	{
		public Config()
		{
			RootDir = ".";
			TmpDir = "temp";
			DbDir = "data";
		}

		public Uri BaseUrl;
		public string TmpDir;
		public string RootDir;
		public string DbDir;
		public TimeSpan RequestDelay = TimeSpan.FromSeconds(15);

		public string ArchiveFile
		{
			get { return Path.Combine(TmpDir, "archive.zip"); }
		}

		public string UpdateTmpDir
		{
			get { return Path.Combine(TmpDir, "update");}
		}
	}
}