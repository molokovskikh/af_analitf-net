using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Config;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class ProducerPromotion : IDocModel
	{
		private Config.Config config;
		private Lazy<string> lazyLocalFilename;

		public ProducerPromotion()
		{
			Catalogs = new List<Catalog>();
			Suppliers = new List<Supplier>();
			Producer = new Producer();
			lazyLocalFilename = new Lazy<string>(() => config.MapToFileProducerPromo(this));
		}

		public virtual uint Id { get; set; }
		public virtual Producer Producer { get; set; }
		public virtual string Name { get; set; }
		public virtual string Annotation { get; set; }
		public virtual uint? PromoFileId { get; set; }
		public virtual string RegionMask { get; set; }
		public virtual IList<Catalog> Catalogs { get; set; }
		public virtual IList<Supplier> Suppliers { get; set; }

		public virtual decimal? RegionMaskDecimal {
			get
			{
				if (String.IsNullOrEmpty(RegionMask))
					return null;
				return Convert.ToUInt64(this.RegionMask);
			}
		}

		public virtual string LocalFilename
		{
			get
			{
				if (config == null)
					return null;
				return lazyLocalFilename.Value;
			}
		}

		public virtual string DisplayName
		{
			get
			{
				return string.Format("Акция {0}: {1}", Producer.Name, Name);
			}
		}

		public virtual void Init(Config.Config config)
		{
			this.config = config;
		}

		public virtual FlowDocument ToFlowDocument()
		{
			var doc = new FlowDocument();
			doc.Blocks.Add(new Paragraph(new Run(String.Format("{0}: {1}", Producer.Name, Name)))
			{
				FontWeight = FontWeights.Bold,
				FontSize = 20,
				TextAlignment = TextAlignment.Center
			});

			if (LocalFilename != null)
			{
				if (Path.GetExtension(LocalFilename).Match(".jpg") || Path.GetExtension(LocalFilename).Match(".png"))
				{
					var bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.UriSource = new Uri(Path.GetFullPath(LocalFilename));
					bitmap.EndInit();
					doc.Blocks.Add(new BlockUIContainer(new Image { Source = bitmap, Stretch = Stretch.None }));
				}
				else if (Path.GetExtension(LocalFilename).Match(".txt"))
				{
					doc.Blocks.Add(new Paragraph(new Run(File.ReadAllText(LocalFilename, Encoding.GetEncoding(1251)))));
				}
			}

			doc.Blocks.Add(new Paragraph(new Run("Описание"))
			{
				FontWeight = FontWeights.Bold
			});
			doc.Blocks.Add(new Paragraph(new Run(Annotation)));

			doc.Blocks.Add(new Paragraph(new Run("Список поставщиков участвующих в акции: ")) { FontWeight = FontWeights.Bold });

			string SuppliersList = "";

			if (Suppliers.Count > 0)
			{
				for (int i = 0; i < Suppliers.Count; i++)
				{
					SuppliersList += Suppliers[i].Name + Environment.NewLine;
				}
				doc.Blocks.Add(new Paragraph(new Run(SuppliersList)) { FontSize = 14 });
			}
			return doc;
		}
	}
}
