using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LocalProducerPromotion
	{
		private string file;
		public ProducerPromotion ProducerPromotion;
		public Config.Config Config;
		public Catalog Catalog;
		public Producer Producer;
		public List<Supplier> Supliers;
		public bool Verbose;

		public LocalProducerPromotion()
		{

		}

		public LocalProducerPromotion(string file = null)
		{
			this.file = file;
		}

		public void Execute(ISession session)
		{
			Catalog = Catalog ?? session.Query<Catalog>().First(x => x.HaveOffers);
			ProducerPromotion = new ProducerPromotion()
			{
				Suppliers = session.Query<Supplier>().Take(5).ToList(),
				Annotation = "Тестовая промоакция производителя",
				Name = "Тестовая промоакция производителя",
				Producer = session.Query<Producer>().First(),
				RegionMask = "0",
				PromoFileId = 1
			};

			if (Verbose)
				Console.WriteLine("Создана промоакция производителя для товара {0}", Catalog.FullName);

			ProducerPromotion.Catalogs.Add(Catalog);
			session.Save(ProducerPromotion);

			if (!String.IsNullOrEmpty(file))
			{
				file = FileHelper.MakeRooted(Path.Combine(GetRoot(".."), file));
			}
			var dir = Path.Combine(Config.RootDir, "producerpromotions");

			if (!string.IsNullOrEmpty(file))
			{
				Directory.CreateDirectory(dir);
			}

			File.Copy(file, Path.Combine(dir, ProducerPromotion.Id + Path.GetExtension(file)), true);

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
