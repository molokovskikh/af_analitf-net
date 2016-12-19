using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using AnalitF.Net.Client.Models.Print;
using Common.Tools;
using NHibernate.Util;

namespace AnalitF.Net.Client.Helpers
{
	public static class PrintHelper
	{
		public static IDocumentPaginatorSource ToFixedDocument(DocumentPaginator paginator)
		{
			var outputXps = Path.GetTempFileName();
			paginator.SaveToXps(outputXps);
			var xpsDoc = new XpsDocument(outputXps, FileAccess.Read);
			return xpsDoc.GetFixedDocumentSequence();
		}

		public static void SaveToXps(this FlowDocument document, string filename)
		{
			((IDocumentPaginatorSource)document).DocumentPaginator.SaveToXps(filename);
		}

		public static void SaveToXps(this DocumentPaginator paginator, string filename)
		{
			using (var stream = File.Create(filename)) {
				var factory = new XpsSerializerFactory();
				var writer = factory.CreateSerializerWriter(stream);
				writer.Write(paginator);
			}
		}

		public static string ToRtfString(FlowDocument doc, PageOrientation orientation)
		{
			var magicRtfLandscape = @"{\*\pgdsctbl {\pgdsc0\pgdscuse195\lndscpsxn\pgwsxn16838\pghsxn11906\marglsxn1134\margrsxn567\margtsxn567\margbsxn567\pgdscnxt0}} \formshade{\*\pgdscno0}\landscape\paperh11906\paperw16838\margl1134\margr567\margt567\margb567\sectd\sbknone\sectunlocked1\lndscpsxn\pgndec\pgwsxn16838\pghsxn11906\marglsxn1134\margrsxn567\margtsxn567\margbsxn567\ftnbj\ftnstart1\ftnrstcont\ftnnar\aenddoc\aftnrstcont\aftnstart1\aftnnrlc";
			var ms = new MemoryStream();

			TextRange text = new TextRange(doc.ContentStart, doc.ContentEnd);
			text.Save(ms, DataFormats.Rtf);

			var rtfString = Encoding.Default.GetString(ms.ToArray());

			if (orientation == PageOrientation.Landscape)
			{
				var langPos = rtfString.IndexOf(@"{\lang", StringComparison.Ordinal);
				if (langPos != -1)
				{
					rtfString = rtfString.Insert(langPos, magicRtfLandscape);
				}
			}
			return rtfString;
		}

		public static RenderTargetBitmap ToBitmap(DocumentPaginator dp, int pageNum, bool contentOnly)
		{
			var page = dp.GetPage(pageNum);
			var pagecontent = page.Visual.Descendants<UIElement>().First() as FrameworkElement;
			var pageSize = new Size(pagecontent.ActualWidth, pagecontent.ActualHeight);
			var pageOffset = new Point(pagecontent.Margin.Left, pagecontent.Margin.Top);
			var drawingVisual = new DrawingVisual();
			using (var drawingContext = drawingVisual.RenderOpen()) {
				var visualBrush = new VisualBrush(pagecontent);
				drawingContext.DrawRectangle(visualBrush, null, new Rect(pageOffset, pageSize));
			}
			if (contentOnly) {
				pageSize = new Size(pagecontent.ActualWidth, pagecontent.ActualHeight);
				drawingVisual.Offset = new Vector(-pageOffset.X, -pageOffset.Y);
			}
			else {
				pageSize = page.Size;
			}
			var renderBmp = new RenderTargetBitmap((int) pageSize.Width, (int) pageSize.Height, 96d, 96d, PixelFormats.Pbgra32);
			renderBmp.Render(drawingVisual);
			var dWidth = (int)pageSize.Width;
			var dHeight = (int)pageSize.Height;
			var dStride = dWidth * 4;
			byte[] pixels = new byte[dHeight * dStride];
			for (var j = 0; j < pixels.Length; j++) {
				pixels[j] = 0xFF;
			}
			var bg = BitmapSource.Create(dWidth, dHeight, 96, 96, PixelFormats.Pbgra32, null, pixels, dStride);
			var dv = new DrawingVisual();
			var dc = dv.RenderOpen();
			dc.DrawImage(bg, new Rect(0, 0, dWidth, dHeight));
			dc.DrawImage(renderBmp, new Rect(0, 0, dWidth, dHeight));
			dc.Close();
			var resultBitmap = new RenderTargetBitmap((int)dWidth, (int)dHeight, 96d, 96d, PixelFormats.Pbgra32);
			resultBitmap.Render(dv);
			return resultBitmap;
		}

		public static RenderTargetBitmap ToBitmap(WrapDocumentPaginator dp, int pageNum, bool contentOnly)
		{
			var page = dp.GetPage(pageNum);
			var pagevisual = page.Visual as ContainerVisual;
			var pageSize = page.Size;
			if (contentOnly) {
				var rects = new List<Rect>();
				foreach (var item in pagevisual.Children) {
					var container = item as ContainerVisual;
					var offset = container.Transform;
					var containerItem = container.Descendants<ContainerVisual>().ToArray()[1];
					var rect = containerItem.DescendantBounds;
					rect.Offset(offset.Value.OffsetX, offset.Value.OffsetY);
					rects.Add(rect);
				}
				var pt = new Point {
					X = rects.Min(r => r.X),
					Y = rects.Min(r => r.Y)
				};
				pt.X = pt.X < 0 ? 0 : pt.X;
				pt.Y = pt.Y < 0 ? 0 : pt.Y;
				var maxWidth = rects.Max(r => (r.X + r.Width)) + 2;
				var maxHeight = rects.Max(r => (r.Y + r.Height)) + 2;
				var size = new Size(maxWidth - pt.X, maxHeight - pt.Y);
				pageSize = size;
				pagevisual.Offset = new Vector(-pt.X, -pt.Y);
			}
			var renderBmp = new RenderTargetBitmap((int)pageSize.Width, (int)pageSize.Height, 96d, 96d, PixelFormats.Pbgra32);
			renderBmp.Render(pagevisual);
			var dWidth = (int)pageSize.Width;
			var dHeight = (int)pageSize.Height;
			var dStride = dWidth * 4;
			byte[] pixels = new byte[dHeight * dStride];
			for (var j = 0; j < pixels.Length; j++)
			{
				pixels[j] = 0xFF;
			}
			var bg = BitmapSource.Create(dWidth, dHeight, 96, 96, PixelFormats.Pbgra32, null, pixels, dStride);
			var dv = new DrawingVisual();
			var dc = dv.RenderOpen();
			dc.DrawImage(bg, new Rect(0,0, dWidth, dHeight));
			dc.DrawImage(renderBmp, new Rect(0,0, dWidth, dHeight));
			dc.Close();
			var resultBitmap = new RenderTargetBitmap((int)dWidth, (int)dHeight, 96d, 96d, PixelFormats.Pbgra32);
			resultBitmap.Render(dv);
			return resultBitmap;
		}

		public static PrintQueueCollection GetPrinters()
		{
			var server = new LocalPrintServer();
			var printQueues =
				server.GetPrintQueues(new[] {
					EnumeratedPrintQueueTypes.Local,
					EnumeratedPrintQueueTypes.Connections
				});
			return printQueues;
		}
	}
}