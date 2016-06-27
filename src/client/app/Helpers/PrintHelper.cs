using System;
using System.IO;
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

		public static RenderTargetBitmap ToBitmap(DocumentPaginator dp)
		{
			var cv = new ContainerVisual();

			double offsetHeight = 0;

			for (var i = 0; i < dp.PageCount; i++)
			{
				DocumentPage p = dp.GetPage(i);
				ContainerVisual v = p.Visual as ContainerVisual;
				v.Transform = new TranslateTransform(0, offsetHeight);
				offsetHeight += p.Size.Height;
				cv.Children.Add(v);
			}

			Size pageFullSize = new Size(dp.PageSize.Width, dp.PageSize.Height * dp.PageCount);

			var renderBitmap =
					new RenderTargetBitmap(
					(int)pageFullSize.Width,
					(int)pageFullSize.Height,
					96d,
					96d,
					PixelFormats.Pbgra32
			);

			renderBitmap.Render(cv);

			int dWidth = (int)pageFullSize.Width;
			int dHeight = (int)pageFullSize.Height;
			int dStride = dWidth * 4;

			byte[] pixels = new byte[dHeight * dStride];

			for (int i = 0; i < pixels.Length; i++)
			{
					pixels[i] = 0xFF;
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

			DrawingVisual dv = new DrawingVisual();
			DrawingContext dc = dv.RenderOpen();
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