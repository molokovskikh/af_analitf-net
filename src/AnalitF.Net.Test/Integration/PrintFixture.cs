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
			view.Offers = new List<Offer> {
				new Offer { ProducerSynonym = "123", ProductSynonym = "123", Price = new Price() }
			};
			var result = view.Print();
			Assert.That(result.Doc, Is.Not.Null);
		}

		public static void SaveAndOpen(PrintResult result)
		{
			var file = "output.xps";
			var paginator = new WrapDocumentPaginator(((IDocumentPaginatorSource)result.Doc).DocumentPaginator);
			paginator.SaveToXps(file);
			Process.Start(file);
		}

		public static void SaveToBmp(Visual visual, string file)
		{
			var bmp = new RenderTargetBitmap(180, 180, 120, 96, PixelFormats.Pbgra32);
			bmp.Render(visual);
			var enc = new BmpBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bmp));
			using (var f = File.OpenWrite(file))
				enc.Save(f);
		}
	}
}