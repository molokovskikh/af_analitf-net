using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LocalPromotion
	{
		private string file;
		public Promotion Promotion;
		public Config.Config Config;

		public LocalPromotion()
		{
		}

		public LocalPromotion(string file = null)
		{
			this.file = file;
		}

		public void Execute(ISession session)
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			Promotion = new Promotion {
				Supplier = session.Query<Supplier>().First(),
				Name = "Тестовая промо-акция",
				Annotation = "Тестовая промо-акция"
			};
			Promotion.Catalogs.Add(catalog);
			session.Save(Promotion);
			if (!String.IsNullOrEmpty(file)) {
				if (!File.Exists(file)) {
					file = FileHelper.MakeRooted(Path.Combine(GetRoot(".."), file));
				}
				var dir = Path.Combine(Config.RootDir, "promotions");
				if (!string.IsNullOrEmpty(file)) {
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
				}
				File.Copy(file, Path.Combine(dir, Promotion.Id + Path.GetExtension(file)));
			}
		}

		private string GetRoot(string path)
		{
			if (Directory.Exists(Path.Combine(path, "src")))
				return path;
			else
				return GetRoot(Path.Combine(path, ".."));
		}
	}
}