using System.IO;
using System.Windows.Documents;
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
	}
}