using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels
{
	public class PrintPreviewViewModel : Screen
	{
		public PrintPreviewViewModel()
		{
			DisplayName = "Предварительный просмотр";
		}

		public PrintPreviewViewModel(PrintResult result)
			: this()
		{
			if (result == null)
				return;

			var paginator = new WrapDocumentPaginator(((IDocumentPaginatorSource)result.Doc).DocumentPaginator);
			var outputXps = Path.GetTempFileName();
			paginator.SaveToXps(outputXps);
			var xpsDoc = new XpsDocument(outputXps, FileAccess.Read);
			Document = xpsDoc.GetFixedDocumentSequence();
		}

		public IDocumentPaginatorSource Document { get; set; }
	}
}