using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Print;
using Caliburn.Micro;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Results
{
	public class PrintResult : IResult
	{
		private string name;

		public PrintResult(FlowDocument doc, string name)
		{
			Doc = doc;
			Docs = new [] { doc};
			Prepare(doc);
			this.name = name;
		}

		public PrintResult(IEnumerable<FlowDocument> docs, string name)
		{
			Docs = docs.ToArray();
			Doc = Docs.FirstOrDefault();
			Docs.Each(Prepare);
			this.name = name;
		}

		public FlowDocument Doc { get; private set; }

		public IEnumerable<FlowDocument> Docs { get; private set; }

		private void Prepare(FlowDocument doc)
		{
			if (doc != null) {
				Doc.PagePadding = new Thickness(25);
				Doc.ColumnGap = 0;
				Doc.ColumnWidth = double.PositiveInfinity;
			}
		}

		public void Execute(ActionExecutionContext context)
		{
			if (Docs == null)
				return;

			var dialog = new PrintDialog();
			if (dialog.ShowDialog() != true)
				return;

			foreach (var doc in Docs) {
				var documentPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
				documentPaginator = new WrapDocumentPaginator(documentPaginator);
				if (documentPaginator.PageSize.Width > documentPaginator.PageSize.Height)
					dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
				dialog.PrintDocument(documentPaginator, name);
			}

			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}