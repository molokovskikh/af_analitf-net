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
		private string name = "";
		private IEnumerable<FlowDocument> docs = new FlowDocument[0];
		private BaseDocument baseDocument = new DefaultDocument();

		public PrintResult(string name, BaseDocument doc)
		{
			baseDocument = doc;
			docs = new [] { doc.Build() };
			docs.Each(Prepare);
			this.name = name;
		}

		public PrintResult(string name, params FlowDocument[] docs)
		{
			this.docs = docs;
			this.docs.Each(Prepare);
			this.name = name;
		}

		public PrintResult(string name, IEnumerable<FlowDocument> docs)
		{
			this.docs = docs.ToArray();
			this.docs.Each(Prepare);
			this.name = name;
		}

		private void Prepare(FlowDocument doc)
		{
			if (doc != null) {
				doc.PagePadding = new Thickness(25);
				doc.ColumnGap = 0;
				doc.ColumnWidth = double.PositiveInfinity;
			}
		}

		public void Execute(ActionExecutionContext context)
		{
			if (docs == null)
				return;

			var dialog = new PrintDialog();
			if (dialog.ShowDialog() != true)
				return;

			foreach (var doc in docs) {
				var documentPaginator = GetPaginator(doc);
				if (documentPaginator.PageSize.Width > documentPaginator.PageSize.Height)
					dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
				dialog.PrintDocument(documentPaginator, name);
			}

			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public WrapDocumentPaginator Paginator
		{
			get
			{
				var doc = docs.First();
				return GetPaginator(doc);
			}
		}

		private WrapDocumentPaginator GetPaginator(FlowDocument doc)
		{
			var documentPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			var defaultDocument = baseDocument as DefaultDocument;
			if (defaultDocument != null) {
				defaultDocument.Document = doc;
			}
			return new WrapDocumentPaginator(documentPaginator, baseDocument);
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}