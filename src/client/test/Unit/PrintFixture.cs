using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
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
			var model = new CatalogOfferViewModel(catalog);
			model.CurrentCatalog.Value = catalog;
			model.Offers.Value = Offers();

			var result = model.PrintPreview().GetEnumerator();
			result.MoveNext();
			var preview = (DialogResult)result.Current;
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_price()
		{
			var price = new Price {
				PriceDate = DateTime.Now,
				Name = "Тестовый поставщик",
				Phone = "473-2606000"
			};
			var address = new Address("Тестовый адрес доставки");
			var doc = new PriceOfferDocument(Offers(), price, address).Build();
			Assert.That(doc, Is.Not.Null);
		}

		[Test]
		public void Print_in_landscape_mode()
		{
			var doc = new RejectsDocument(Enumerable.Repeat(1, 100).Select(i => new Reject()).ToList(), false).Build();
			Assert.IsNotNull(doc);
			var size = ((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize;
			//816 - отступы
			Assert.AreEqual(716, size.Height);
			Assert.AreEqual(1056, size.Width);
		}

		[Test]
		public void Build_selected_pages()
		{
			var baseDoc = new RejectsDocument(Enumerable.Repeat(1, 100).Select(i => new Reject()).ToList(), false);
			var flowDoc = baseDoc.Build();
			var paginator = new WrapDocumentPaginator(flowDoc, baseDoc, PageRangeSelection.UserPages, new PageRange(1, 1));
			paginator.GetPage(0);
			Assert.AreEqual(paginator.PageCount, 1);
		}

		[Test]
		public void Validate_range()
		{
			var baseDoc = new RejectsDocument(Enumerable.Repeat(1, 100).Select(i => new Reject()).ToList(), false);
			var flowDoc = baseDoc.Build();
			var paginator = new WrapDocumentPaginator(flowDoc, baseDoc, PageRangeSelection.UserPages, new PageRange(1, 100));
			paginator.GetPage(0);
			Assert.AreEqual(2, paginator.PageCount);
		}

		[Test]
		public void Invalidate_cache()
		{
			var baseDoc = new RejectsDocument(Enumerable.Repeat(1, 100).Select(i => new Reject()).ToList(), false);
			var flowDoc = baseDoc.Build();
			var paginator = new WrapDocumentPaginator(flowDoc, baseDoc, PageRangeSelection.UserPages, new PageRange(1, 100));
			paginator.GetPage(0);
			Assert.AreEqual(2, paginator.PageCount);

			paginator = new WrapDocumentPaginator(flowDoc, baseDoc, PageRangeSelection.UserPages, new PageRange(1, 100));
			paginator.GetPage(0);
			Assert.AreEqual(2, paginator.PageCount);
		}

		[Test]
		public void Print_price_tag()
		{
			var address = new Address("Тестовый");
			var lines = new List<TagPrintable>();
			var line = new TagPrintable() {
				Nds = 10,
				RetailCost = 251.20m,
				Product = "Диклофенак",
				Quantity = 1
			};
			lines.Add(line);

			var priceTagSettings = new PriceTagSettings() {Type = PriceTagType.Normal};
			var doc = new PriceTagDocument(lines, priceTagSettings, null).Build();
			Assert.IsNotNull(doc);

			var priceTag = PriceTag.Default(TagType.RackingMap, null);
			var settings = new Settings(address);
			settings.RackingMap.Size = RackingMapSize.Custom;
			var doc2 = new RackingMapDocument(lines, settings, priceTag).Build();
			Assert.IsNotNull(doc2);
		}

		[Test]
		public void Check_price_tag_address()
		{
			var address = new Address("Тестовый адрес");
			var lines = new List<TagPrintable>();
			var line = new TagPrintable()
			{
				ClientName = "Тестовый клиент",
				Nds = 10,
				RetailCost = 251.20m,
				Product = "Диклофенак",
				Quantity = 1
			};
			lines.Add(line);

			var priceTagSettings = new PriceTagSettings()
			{
				Type = PriceTagType.Normal,
				Address = address
			};
			var priceTag = PriceTag.Default(TagType.PriceTag, address);
			var doc = new PriceTagDocument(lines, priceTagSettings, priceTag).Build();
			Assert.IsNotNull(doc);

			var text = doc.Descendants<StackPanel>()
				.First()
				.Descendants<StackPanel>()
				.First()
				.Children[0]
				.Descendants<TextBlock>()
				.First()
				.Text;
			Assert.AreEqual($"{line.ClientName}, {address.Name}", text);
		}

		[Test]
		public void Check_custom_price_tag_address()
		{
			var address = new Address("Тестовый адрес");
			var lines = new List<TagPrintable>();
			var line = new TagPrintable()
			{
				ClientName = "Тестовый клиент",
				Nds = 10,
				RetailCost = 251.20m,
				Product = "Диклофенак",
				Quantity = 1
			};
			lines.Add(line);

			var priceTagSettings = new PriceTagSettings()
			{
				Type = PriceTagType.Custom,
				Address = address
			};
			var priceTag = PriceTag.Default(TagType.PriceTag, address);
			var doc = new PriceTagDocument(lines, priceTagSettings, priceTag).Build();
			Assert.IsNotNull(doc);

			var text = doc.Descendants<StackPanel>()
				.First()
				.Descendants<Label>()
				.First()
				.Content.ToString().Trim();
			Assert.AreEqual($"{line.ClientName}, {address.Name}", text);
		}

		[Test]
		public void Build_all_tags()
		{
			var lines = new List<TagPrintable>();
			for(var i = 0; i < 25; i++) {
				var line = new TagPrintable() {
					Nds = 10,
					RetailCost = 251.20m,
					Product = "Диклофенак",
					Quantity = 1
				};
				lines.Add(line);
			}

			var priceTagSettings = new PriceTagSettings() { Type = PriceTagType.Normal };
			var doc = new PriceTagDocument(lines, priceTagSettings, null).Build();

			Assert.IsNotNull(doc);
			Assert.AreEqual(2, doc.Pages.Count);

			var page1 = doc.Pages[0].Child;
			Assert.AreEqual(24, page1.Descendants<Grid>().First().Children.Count);
			var page2 = doc.Pages[1].Child;
			Assert.AreEqual(1, page2.Descendants<Grid>().First().Children.Count);
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

		public static void SaveToPng(FrameworkElement el, string file)
		{
			SaveToPng(el, file, new Size(el.ActualWidth, el.ActualHeight));
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
			var dir = Path.GetDirectoryName(file);
			if (!String.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);
			using (var f = File.OpenWrite(file))
				enc.Save(f);
		}
	}
}