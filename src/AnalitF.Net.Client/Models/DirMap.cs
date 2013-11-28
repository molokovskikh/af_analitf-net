using System.IO;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class DirMap : BaseNotify
	{
		private string _dir;

		public DirMap()
		{
		}

		public DirMap(Settings settings, Supplier supplier)
		{
			Supplier = supplier;
			Dir = Path.Combine(settings.MapPath("waybills"), FileHelper.StringToPath(Supplier.Name));
		}

		public virtual uint Id { get; set; }
		public virtual Supplier Supplier { get; set; }

		public virtual string Dir
		{
			get { return _dir; }
			set
			{
				if (!Equals(_dir, value)) {
					_dir = value;
					OnPropertyChanged();
				}
			}
		}
	}
}