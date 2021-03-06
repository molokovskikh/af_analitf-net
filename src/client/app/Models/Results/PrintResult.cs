using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Print;
using Caliburn.Micro;
using Common.Tools;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models.Results
{
	public class PrintResult : IResult
	{
		private string name = "";
		private IEnumerable<Lazy<Tuple<FlowDocument, BaseDocument>>> docs
			= new Lazy<Tuple<FlowDocument, BaseDocument>>[0];

		private Lazy<Tuple<FlowDocument, BaseDocument>>[] buffered;

		public string PrinterName { get; set; }

		public PrintResult(string name, IEnumerable<BaseDocument> docs, string printerName = "")
		{
			this.docs = docs
				.Select(b => new Lazy<Tuple<FlowDocument, BaseDocument>>(() => Tuple.Create(b.Build(), b)));
			this.name = name;
			PrinterName = printerName;
		}

		public PrintResult(string name, params BaseDocument[] baseDocs)
			: this(name, (IEnumerable<BaseDocument>)baseDocs)
		{
		}

		public PrintResult(string name, params FlowDocument[] docs)
			: this(name, docs.Select(d => new DefaultDocument(d)))
		{
		}

		private void Prepare(FlowDocument doc)
		{
			if (doc != null) {
				doc.ColumnGap = 0;
				doc.ColumnWidth = double.PositiveInfinity;
			}
		}

		public void Execute(ActionExecutionContext context)
		{
			var dialog = new PrintDialog();
			if (string.IsNullOrEmpty(PrinterName)) {
				dialog.UserPageRangeEnabled = true;
			if (dialog.ShowDialog() != true)
				return;
			} else {
				dialog.PrintQueue = new PrintQueue(new PrintServer(), PrinterName);
			}

			foreach (var doc in Docs) {
				var flowDocument = doc.Value.Item1;
				var paginator = GetPaginator(flowDocument, doc.Value.Item2, dialog.PageRangeSelection, dialog.PageRange);
				SetPageOrientation(dialog, paginator);
				dialog.PrintDocument(paginator, name);
			}

			Completed?.Invoke(this, new ResultCompletionEventArgs());
		}

		public static PageOrientation GetPageOrientation(DocumentPaginator documentPaginator)
		{
			if (documentPaginator.PageSize.Width > documentPaginator.PageSize.Height)
				return PageOrientation.Landscape;
			return PageOrientation.Unknown;
		}

		public DocumentPaginator Paginator => GetPaginator(PageRangeSelection.AllPages, new PageRange());

		public DocumentPaginator GetPaginator(PrintDialog dialog)
		{
			var paginator = GetPaginator(dialog.PageRangeSelection, dialog.PageRange);
			SetPageOrientation(dialog, paginator);
			return paginator;
		}

		private void SetPageOrientation(PrintDialog dialog, DocumentPaginator documentPaginator)
		{
			var orientation = GetPageOrientation(documentPaginator);
			if (orientation != PageOrientation.Unknown) {
				dialog.PrintTicket.PageOrientation = orientation;
			}
		}

		public DocumentPaginator GetPaginator(PageRangeSelection selection, PageRange range)
		{
			var doc = Docs.First();
			return GetPaginator(doc.Value.Item1, doc.Value.Item2, selection, range);
		}

		private DocumentPaginator GetPaginator(FlowDocument doc, BaseDocument baseDoc,
			PageRangeSelection selection, PageRange range)
		{
			Prepare(doc);
			return new WrapDocumentPaginator(doc, baseDoc, selection, range);
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public Lazy<Tuple<FlowDocument, BaseDocument>>[] Docs
		{
			get
			{
				return buffered = buffered ?? docs.ToArray();
			}
		}
	}
}