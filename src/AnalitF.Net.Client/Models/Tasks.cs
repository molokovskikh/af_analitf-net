using System;
using System.Net;
using System.Reactive.Subjects;
using System.Threading;
using AnalitF.Net.Client.Models.Commands;

namespace AnalitF.Net.Client.Models
{
	public class Tasks
	{
		public static Uri BaseUri;
		public static string ArchiveFile;
		public static string ExtractPath;
		public static string RootPath;

		public static UpdateResult Import(ICredentials credentials, CancellationToken token,
			BehaviorSubject<Progress> progress)
		{
			var command = new UpdateCommand(ArchiveFile, ExtractPath, RootPath) {
				BaseUri = BaseUri,
				Credentials = credentials,
				Token = token,
				Progress = progress,
				Reporter = new ProgressReporter(progress)
			};
			return command.Process(() => {
				command.Import();
				return UpdateResult.OK;
			});
		}
	}
}