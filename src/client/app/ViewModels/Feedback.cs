using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using Ionic.Zip;
using log4net.Config;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;

namespace AnalitF.Net.Client.ViewModels
{
	public class Feedback : Screen, IDisposable, ICancelable
	{
		public Feedback(Config.Config config)
		{
			this.config = config;
			DisplayName = "Письмо в АК \"Инфорум\"";
			SendLog = true;
			Attachments = new ObservableCollection<string>();
			IsSupport = true;
			WasCancelled = true;
		}

		private Config.Config config;

		public string ArchiveName;
		public bool IsSupport { get; set; }
		public bool IsBilling { get; set; }
		public bool IsOffice { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public bool SendLog { get; set; }
		public ObservableCollection<string> Attachments { get; set; }
		public string CurrentAttachment { get; set; }
		public bool WasCancelled { get; set; }

		public void DeleteAttachment()
		{
			if (String.IsNullOrEmpty(CurrentAttachment))
				return;
			Attachments.Remove(CurrentAttachment);
		}

		public IEnumerable<IResult> AddAttachment()
		{
			var open = new OpenFileResult();
			yield return open;
			Attachments.Add(open.Dialog.FileName);
		}

		public IEnumerable<IResult> Send()
		{
			WasCancelled = false;
			if (Attachments.Count == 0) {
				TryClose();
				yield break;
			}

			var task = new Task(() => {
				if (!String.IsNullOrEmpty(ArchiveName))
					File.Delete(ArchiveName);

				ArchiveName = Path.GetTempFileName();
				var files = Attachments.ToArray();
				if (SendLog)
					files = files.Concat(Directory.GetFiles(config.RootDir, "*.log")).ToArray();

				try {
					log4net.LogManager.ResetConfiguration();
					using(var zip = new ZipFile()) {
						foreach (var attachment in files) {
							zip.AddFile(attachment);
						}
						zip.Save(ArchiveName);
					}
				}
				catch(Exception) {
					File.Delete(ArchiveName);
					throw;
				}
				finally {
					XmlConfigurator.Configure();
				}
				if (new FileInfo(ArchiveName).Length > 4 * 1024 * 1024)
					throw new EndUserError("Размер архива с вложенными файлами превышает 4Мб.");
			});
			yield return new TaskResult(task, new WaitViewModel("Проверка вложений.\r\nПожалуйста подождите."));
			if (task.IsFaulted) {
				var message = task.Exception.GetBaseException().Message;
				yield return MessageResult.Error(message);
			}
			else {
				TryClose();
			}
		}

		public void Cancel()
		{
			TryClose();
		}

		public FeedbackMessage GetMessage()
		{
			var message = new FeedbackMessage {
				IsSupport = IsSupport,
				IsBilling = IsBilling,
				IsOffice = IsOffice,
				Subject = Subject,
				Body = Body,
			};
			if (!String.IsNullOrEmpty(ArchiveName))
				message.Attachments = File.ReadAllBytes(ArchiveName);
			Dispose();
			return message;
		}

		public void Dispose()
		{
			if (!String.IsNullOrEmpty(ArchiveName)) {
				File.Delete(ArchiveName);
				ArchiveName = null;
			}
		}
	}
}