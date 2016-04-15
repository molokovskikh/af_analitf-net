using System.IO;
using System.Linq;

namespace AnalitF.Net.Service.Models
{
	public class ProducerPromotion
	{
		public virtual int Id { get; set; }
		public virtual string Type { get; set; }

		public virtual string GetFilename(Config.Config config)
		{
			return  Path.Combine(config.ProducerPromotionsPath , Id + "." + Type);
		}

		public virtual string GetArchiveName(string localfilename)
		{
			return "ProducerPromotions/" + Path.GetFileName(localfilename);
		}
	}
}
