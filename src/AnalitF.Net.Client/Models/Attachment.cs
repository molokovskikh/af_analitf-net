using System;
using System.Drawing;
using System.IO;
using System.Net.Http.Handlers;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using NHibernate.Engine;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public enum DownloadState
	{
		NotLoaded,
		Loading,
		Loaded,
		Faulted,
	}

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
			get { return "Не удалось загрузить вложение. Проверьте подключение к Интернет."; }
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

		public abstract string GetLocalFilename(Config.Config config);
		public abstract object GetId();
	}

	public class Attachment : Loadable
	{
		public Attachment()
		{
		}

		public Attachment(string name, long size)
		{
			Name = name;
			Size = size;
		}

		public virtual uint Id { get; set; }
		public virtual string Name { get; set; }
		public virtual long Size { get; set; }
		//review - в базе хранятся абсолютные пути что будет если приложение будет перемещено
		public virtual string LocalFilename { get; set; }

		public virtual string Details
		{
			get
			{
				if (IsDownloaded)
					return string.Format("{0} - {1}, нажмите что бы открыть", Name, Util.HumanizeSize(Size));
				if (IsDownloading)
					return string.Format("{0} - {1}, нажмите для отмены", Name, Util.HumanizeSize(Size));
				return string.Format("{0} - {1}, нажмите для загрузки", Name, Util.HumanizeSize(Size));
			}
		}

		//todo - нужно реализовать кеширование и грузить иконки асинхронно
		public virtual ImageSource FileTypeIcon
		{
			get
			{
				if (String.IsNullOrEmpty(LocalFilename))
					return null;

				var icon = Icon.ExtractAssociatedIcon(LocalFilename);
				return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
					new Int32Rect(0, 0, icon.Width, icon.Height),
					BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
			}
		}

		public override object GetId()
		{
			return Id;
		}

		public override string GetLocalFilename(Config.Config config)
		{
			//нужно нормализовать путь тк Icon.ExtractAssociatedIcon ну будет работать если путь не будет нормализован
			return Path.GetFullPath(Path.Combine(config.RootDir, "attachments", Id + Path.GetExtension(Name)));
		}

		public virtual JournalRecord UpdateLocalFile(string filename)
		{
			LocalFilename = Path.GetFullPath(filename);
			IsDownloaded = true;
			return new JournalRecord(this);
		}

		public virtual JournalRecord UpdateLocalFile(Config.Config config)
		{
			return UpdateLocalFile(GetLocalFilename(config));
		}
	}
}