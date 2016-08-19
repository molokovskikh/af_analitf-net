using System;
using System.Linq;
using System.Printing;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PrintPreviewViewModel : Screen
	{
		private PrintResult result;
		private DocumentViewer2 documentViewer;

		public PrintPreviewViewModel()
		{
			DisplayName = "Предварительный просмотр";
		}

		public PrintPreviewViewModel(PrintResult result)
			: this()
		{
			if (result == null)
				return;
			this.result = result;
			var paginator = result.Paginator;
			Orientation = PrintResult.GetPageOrientation(paginator);
			Document = PrintHelper.ToFixedDocument(paginator);
		}

		public IDocumentPaginatorSource Document { get; set; }
		public PageOrientation Orientation { get; set; }

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			documentViewer = ((DependencyObject)view).Descendants<DocumentViewer2>().First();
			//нужно для реализации функционала выбора страниц печати, подробней смотри комментарий к полю
			documentViewer.PrintResult = result;
		}

		public override void CanClose(Action<bool> callback)
		{
			if (documentViewer.Busy) {
				return;
			}
			callback(!documentViewer.Busy);
		}
	}
}