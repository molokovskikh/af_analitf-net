using System.IO;
using System.Linq;

namespace AnalitF.Net.Service.Models
{
	public class Promotion
	{
		public virtual uint Id { get; set; }

		public virtual string GetFilename(Config.Config config)
		{
			return Directory.GetFiles(config.PromotionsPath, Id + ".*").FirstOrDefault();
		}

		public virtual string GetArchiveName(string localfilename)
		{
			return "promotions/" + Path.GetFileName(localfilename);
		}
	}
}