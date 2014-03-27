﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class PrintFixture : BaseUnitFixture
	{
		[Test]
		public void Print()
		{
			var catalog = new Catalog("Папаверин");
			var view = new CatalogOfferViewModel(catalog);
			view.CurrentCatalog = catalog;
			view.Offers.Value = Offers();
			var result = view.Print();
			Assert.That(result.Paginator, Is.Not.Null);
			var doc = (FlowDocument)result.Paginator.Source;
			var asText = FlowDocumentToText(doc);
			Assert.AreEqual("Папаверин\r\n" +
				"|Наименование|Производитель|Прайс-лист|Срок год.|Дата пр.|Разн.|Цена\r\n" +
				"|Папаверин|ВоронежФарм|||01.01.0001 0:00:00||0\r\n" +
				"Общее количество предложений: 1", asText);
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
			var doc = new PriceOfferDocument(Offers(), price, address).Build();
			Assert.That(doc, Is.Not.Null);
		}

		[Test]
		public void Print_in_landscape_mode()
		{
			var doc = new RejectsDocument(Enumerable.Repeat(1, 100).Select(i => new Reject()).ToList(), false).Build();
			Assert.IsNotNull(doc);
		}

		private static string FlowDocumentToText(FlowDocument doc)
		{
			var builder = new StringBuilder();
			foreach (var el in doc.Descendants().Distinct()) {
				if (el is Paragraph && !(((Paragraph)el).Parent is TableCell)) {
					builder.AppendLine();
				}
				if (el is Run) {
					builder.Append((((Run)el).Text ?? "").Trim());
				}
				if (el is TableRow) {
					builder.AppendLine();
				}
				if (el is TableCell) {
					builder.Append("|");
				}
			}
			return builder.ToString().Trim();
		}


		private static List<Offer> Offers()
		{
			return new List<Offer> {
				new Offer { ProductSynonym = "Папаверин", ProducerSynonym = "ВоронежФарм", Price = new Price() }
			};
		}

		public static void SaveAndOpen(PrintResult result)
		{
			var file = "output.xps";
			var paginator = result.Paginator;
			paginator.SaveToXps(file);
			Process.Start(file);
		}

		public static void SaveToPng(FlowDocument doc)
		{
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			var count = paginator.PageCount;
			for(var i = 0; i <= count; i++) {
				var page = paginator.GetPage(i);
				SaveToPng(page.Visual, i + ".png", page.Size);
			}
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