using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http.Handlers;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
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
		//todo - в базе хранятся абсолютные пути что будет если приложение будет перемещено
		public virtual string LocalFilename { get; set; }

		public virtual string Details
		{
			get
			{
				if (IsDownloaded)
					return $"{Name} - {Util.HumanizeSize(Size)}, нажмите что бы открыть";
				if (IsDownloading)
					return $"{Name} - {Util.HumanizeSize(Size)}, нажмите для отмены";
				return $"{Name} - {Util.HumanizeSize(Size)}, нажмите для загрузки";
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

		public override uint GetId()
		{
			return Id;
		}

		public override string GetLocalFilename(string archEntryName, Config.Config config)
		{
			//нужно нормализовать путь тк Icon.ExtractAssociatedIcon ну будет работать если путь не будет нормализован
			return Path.GetFullPath(Path.Combine(config.RootDir, "attachments", Id + Path.GetExtension(Name)));
		}

		public override JournalRecord UpdateLocalFile(string filename)
		{
			LocalFilename = Path.GetFullPath(filename);
			IsDownloaded = true;
			return new JournalRecord(this, Name, filename);
		}

		public override IEnumerable<string> GetFiles()
		{
			if (!String.IsNullOrEmpty(LocalFilename)) {
				yield return LocalFilename;
			}
		}

		public override string ToString()
		{
			return $"Id: {Id}, Name: {Name}";
		}
	}
}