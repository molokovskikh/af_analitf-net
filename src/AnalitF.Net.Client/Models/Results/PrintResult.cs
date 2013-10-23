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

		public PrintResult(string name, IEnumerable<BaseDocument> docs)
		{
			this.docs = docs
				.Select(b => new Lazy<Tuple<FlowDocument, BaseDocument>>(() => Tuple.Create(b.Build(), b)));
			this.name = name;
		}

		public PrintResult(string name, params BaseDocument[] baseDocs)
			: this(name, (IEnumerable<BaseDocument>)baseDocs)
		{
		}

		public PrintResult(string name, params FlowDocument[] docs)
			: this(name, (IEnumerable<FlowDocument>)docs)
		{
		}

		public PrintResult(string name, IEnumerable<FlowDocument> docs)
			: this(name, docs.Select(d => new DefaultDocument(d)))
		{
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
			var dialog = new PrintDialog();
			if (dialog.ShowDialog() != true)
				return;

			CheckBuffer();
			foreach (var doc in buffered) {
				var documentPaginator = GetPaginator(doc.Value.Item1, doc.Value.Item2);
				var orientation = GetPageOrientation(documentPaginator);
				if (orientation != PageOrientation.Unknown)
					dialog.PrintTicket.PageOrientation = orientation;
				dialog.PrintDocument(documentPaginator, name);
			}

			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public static PageOrientation GetPageOrientation(DocumentPaginator documentPaginator)
		{
			if (documentPaginator.PageSize.Width > documentPaginator.PageSize.Height)
				return PageOrientation.Landscape;
			return PageOrientation.Unknown;
		}

		public WrapDocumentPaginator Paginator
		{
			get
			{
				CheckBuffer();
				var doc = buffered.First();
				return GetPaginator(doc.Value.Item1, doc.Value.Item2);
			}
		}

		private void CheckBuffer()
		{
			if (buffered == null)
				buffered = docs.ToArray();
		}

		private WrapDocumentPaginator GetPaginator(FlowDocument doc, BaseDocument baseDoc)
		{
			Prepare(doc);
			var documentPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			return new WrapDocumentPaginator(documentPaginator, baseDoc);
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}