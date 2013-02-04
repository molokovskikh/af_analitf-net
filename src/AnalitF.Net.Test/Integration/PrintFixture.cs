using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class PrintFixture : BaseFixture
	{
		[Test]
		public void Print()
		{
			var view = new CatalogOfferViewModel(new Catalog("тест"));
			view.CurrentCatalog = new Catalog { Name = new CatalogName(), Form = "Папаверин" };
			view.Offers = Offers();
			var result = view.Print();
			Assert.That(result.Doc, Is.Not.Null);
		}

		[Test]
		public void Print_price()
		{
			var price = new Price {
				PriceDate = DateTime.Now,
				Name = "Тестовый поставщик",
				Phone = "473-2606000"
			};
			var address = new Address { Name = "Тестовый адрес доставки" };
			var doc = new PriceOfferDocument(Offers(), price, address).BuildDocument();
			Assert.That(doc, Is.Not.Null);
		}

		private static List<Offer> Offers()
		{
			return new List<Offer> {
				new Offer { ProducerSynonym = "123", ProductSynonym = "123", Price = new Price() }
			};
		}

		public static void SaveAndOpen(PrintResult result)
		{
			var file = "output.xps";
			var paginator = new WrapDocumentPaginator(((IDocumentPaginatorSource)result.Doc).DocumentPaginator);
			paginator.SaveToXps(file);
			Process.Start(file);
		}

		public static void SaveToPng(UIElement visual, string file, Size size)
		{
			visual.Measure(size);
			visual.Arrange(new Rect(size));
			SaveToPng((Visual)visual, file, size);
		}

		public static void SaveToPng(Visual visual, string file, Size size)
		{
			var bmp = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
			bmp.Render(visual);
			var enc = new PngBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bmp));
			using (var f = File.OpenWrite(file))
				enc.Save(f);
		}
	}
}