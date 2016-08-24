using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using AnalitF.Net.Client.Models.Print;

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

			var rtfString = System.Text.Encoding.Default.GetString(ms.ToArray());

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

		public static RenderTargetBitmap ToBitmap(WrapDocumentPaginator dp, int pageNum, bool contentOnly)
		{
			var page = dp.GetPage(pageNum);
			var pagevisual = page.Visual as ContainerVisual;
			if (pagevisual == null)
				throw new ArgumentException("Нет такой страницы");
			var pageFullSize = page.Size;
			if (contentOnly) {

				ContainerVisual header;

				var pageContentRect = dp.GetRealContentRect(pageNum);

				pageFullSize = pageContentRect.Size;
				pagevisual.Offset = new Vector(-pageContentRect.Location.X, -pageContentRect.Location.Y);
			}

			var renderBitmap =
					new RenderTargetBitmap(
					(int)pageFullSize.Width,
					(int)pageFullSize.Height,
					96d,
					96d,
					PixelFormats.Pbgra32
			);

			renderBitmap.Render(pagevisual);

			var dWidth = (int)pageFullSize.Width;
			var dHeight = (int)pageFullSize.Height;
			var dStride = dWidth * 4;

			byte[] pixels = new byte[dHeight * dStride];

			for (var j = 0; j < pixels.Length; j++)
			{
					pixels[j] = 0xFF;
			}

			var bg = BitmapSource.Create(
					dWidth,
					dHeight,
					96,
					96,
					PixelFormats.Pbgra32,
					null,
					pixels,
					dStride
			);
			var dv = new DrawingVisual();
			var dc = dv.RenderOpen();
			dc.DrawImage(bg, new Rect(0,0, dWidth, dHeight));
			dc.DrawImage(renderBitmap, new Rect(0,0, dWidth, dHeight));
			dc.Close();

			var resultBitmap =
					new RenderTargetBitmap(
					(int)dWidth,
					(int)dHeight,
					96d,
					96d,
					PixelFormats.Pbgra32
			);
			resultBitmap.Render(dv);
			return resultBitmap;
		}
	}
}