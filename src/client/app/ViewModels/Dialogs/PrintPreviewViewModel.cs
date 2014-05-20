using System.Printing;
using System.Windows.Documents;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
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