using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public class JournalRecord
	{
		public JournalRecord()
		{
		}

		public JournalRecord(Attachment attachment)
		{
			CreateAt = DateTime.Now;
			Name = attachment.Name;
			Filename = attachment.LocalFilename;
			RecordId = attachment.Id;
			RecordType = "Attachment";
		}

		public virtual uint Id { get; set; }
		public virtual DateTime CreateAt { get; set; }
		public virtual string Name { get; set; }
		public virtual string Filename { get; set; }
		public virtual string RecordType { get; set; }
		public virtual uint RecordId { get; set; }

		//todo - нужно реализовать кеширование и грузить иконки асинхронно
		public virtual ImageSource FileTypeIcon
		{
			get
			{
				if (String.IsNullOrEmpty(Filename))
					return null;

				var icon = Icon.ExtractAssociatedIcon(Filename);
				return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
					new Int32Rect(0, 0, icon.Width, icon.Height),
					BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
			}
		}
	}
}