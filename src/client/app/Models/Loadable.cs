using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using NHibernate.Engine;

namespace AnalitF.Net.Client.Models
{
	public abstract class Loadable : BaseNotify
	{
		private bool isDownloaded;
		private bool isError;
		private double progress;
		private bool isConnecting;
		private bool isDownloading;

		protected Loadable()
		{
			RequstCancellation = Disposable.Empty;
		}

		[Ignore]
		public virtual IDisposable RequstCancellation { get; set; }

		[Ignore]
		public virtual ISession Session { get; set; }

		[Ignore]
		public virtual EntityEntry Entry { get; set; }

		public virtual string ErrorDetails
		{
			get { return "Не удалось загрузить. Проверьте подключение к Интернет."; }
		}

		[Ignore]
		public virtual bool IsDownloading
		{
			get { return isDownloading; }
			set
			{
				if (isDownloading == value)
					return;

				isDownloading = value;
				if (isDownloading) {
					IsConnecting = true;
					IsError = false;
					IsDownloaded = false;
				}
				else {
					IsConnecting = false;
				}
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual double Progress
		{
			get { return progress; }
			set
			{
				progress = value;
				if (value > 0)
					IsConnecting = false;
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual bool IsConnecting
		{
			get { return isConnecting; }
			protected set
			{
				if (isConnecting == value)
					return;
				isConnecting = value;
				OnPropertyChanged();
			}
		}

		public virtual bool IsError
		{
			get { return isError; }
			set
			{
				if (isError == value)
					return;

				isError = value;
				if (isError) {
					IsDownloaded = false;
					IsConnecting = false;
					IsDownloading = false;
				}
				OnPropertyChanged();
			}
		}

		public virtual bool IsDownloaded
		{
			get { return isDownloaded; }
			set
			{
				if (isDownloaded == value)
					return;

				isDownloaded = value;
				if (isDownloaded) {
					IsError = false;
					IsConnecting = false;
					IsDownloading = false;
				}
				OnPropertyChanged();
			}
		}

		public abstract uint GetId();
		public abstract string GetLocalFilename(string archEntryName, Config.Config config);
		public abstract JournalRecord UpdateLocalFile(string localFileName);
		public abstract IEnumerable<string> GetFiles();

		public virtual void Error()
		{
			IsError = true;
			RequstCancellation.Dispose();
		}

		public virtual void Completed()
		{
			IsDownloading = false;
			RequstCancellation.Dispose();
		}
	}
}