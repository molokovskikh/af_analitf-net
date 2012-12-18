using System.IO;
using System.Windows.Documents;
using System.Windows.Xps.Serialization;

namespace AnalitF.Net.Client.Helpers
{
	public static class FlowDocumentExtentions
	{
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