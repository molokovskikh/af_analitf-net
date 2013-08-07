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
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models.Results
{
	public class PrintResult : IResult
	{
		private string name = "";
		private IEnumerable<Lazy<Tuple<FlowDocument, BaseDocument>>> docs
			= new Lazy<Tuple<FlowDocument, BaseDocument>>[0];

		public PrintResult(string name, IEnumerable<BaseDocument> docs)
		{
			this.docs = docs.Select(b => new Lazy<Tuple<FlowDocument, BaseDocument>>(
				() => Tuple.Create(b.Build(), b)));
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
		{
			this.docs = docs.Select(d => new Lazy<Tuple<FlowDocument, BaseDocument>>(
				() => Tuple.Create(d, (BaseDocument)null)));
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
				var doc = docs.First();
				return GetPaginator(doc.Value.Item1, doc.Value.Item2);
			}
		}

		private WrapDocumentPaginator GetPaginator(FlowDocument doc, BaseDocument baseDoc)
		{
			baseDoc = baseDoc ?? new DefaultDocument {
				Document = doc
			};

			Prepare(doc);
			var documentPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			return new WrapDocumentPaginator(documentPaginator, baseDoc);
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}