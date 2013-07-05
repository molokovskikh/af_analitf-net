using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
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

			var paginator = result.Paginator;
			Orientation = PrintResult.GetPageOrientation(paginator);
			Document = PrintHelper.ToFixedDocument(paginator);
		}

		public IDocumentPaginatorSource Document { get; set; }
		public PageOrientation Orientation { get; set; }
	}
}