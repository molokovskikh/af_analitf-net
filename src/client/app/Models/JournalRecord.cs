using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NHibernate;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public class JournalRecord
	{
		public JournalRecord()
		{
		}

		public JournalRecord(Loadable loadable)
		{
			CreateAt = DateTime.Now;
			RecordId = loadable.GetId();
			RecordType = NHibernateUtil.GetClass(loadable).Name;
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
				if (!File.Exists(Filename))
					return null;

				var icon = Icon.ExtractAssociatedIcon(Filename);
				return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
					new Int32Rect(0, 0, icon.Width, icon.Height),
					BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
			}
		}
	}
}