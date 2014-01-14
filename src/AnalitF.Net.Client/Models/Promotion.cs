using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class Promotion : IDocModel
	{
		private Config.Config config;
		private Lazy<string> lazyLocalFilename;

		public Promotion()
		{
			Catalogs = new List<Catalog>();
			lazyLocalFilename = new Lazy<string>(() => config.MapToFile(this));
		}

		public virtual uint Id { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual string Name { get; set; }
		public virtual string Annotation { get; set; }
		public virtual IList<Catalog> Catalogs { get; set; }

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
				return string.Format("Акция {0}: {1}", Supplier.Name, Name);
			}
		}

		public virtual void Init(Config.Config config)
		{
			this.config = config;
		}

		public virtual FlowDocument ToFlowDocument()
		{
			var doc = new FlowDocument();
			doc.Blocks.Add(new Paragraph(new Run(String.Format("{0}: {1}", Supplier.Name, Name))) {
				FontWeight = FontWeights.Bold,
				FontSize = 20,
				TextAlignment = TextAlignment.Center
			});

			if (LocalFilename != null) {
				if (Path.GetExtension(LocalFilename).Match(".jpg")) {
					var bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.UriSource = new Uri(LocalFilename);
					bitmap.EndInit();
					doc.Blocks.Add(new BlockUIContainer(new Image { Source = bitmap }));
				}
				else if (Path.GetExtension(LocalFilename).Match(".txt")) {
					doc.Blocks.Add(new Paragraph(new Run(File.ReadAllText(LocalFilename))));
				}
			}

			doc.Blocks.Add(new Paragraph(new Run("Описание")) {
				FontWeight = FontWeights.Bold
			});
			doc.Blocks.Add(new Paragraph(new Run(Annotation)));
			return doc;
		}
	}
}