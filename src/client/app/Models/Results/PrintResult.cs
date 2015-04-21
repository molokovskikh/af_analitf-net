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
				doc.ColumnGap = 0;
				doc.ColumnWidth = double.PositiveInfinity;
			}
		}

		public void Execute(ActionExecutionContext context)
		{
			var dialog = new PrintDialog();
			dialog.UserPageRangeEnabled = true;
			if (dialog.ShowDialog() != true)
				return;

			foreach (var doc in Docs) {
				var flowDocument = doc.Value.Item1;
				//очередное безумие - лазерные принтеры не могут печатать на всей поверхности листа и сообщают об этом
				//через GetPrintCapabilities
				//wpf будет пытаться печатать на страницы абстрактного размера игнорируя то что говорит ему принтер
				//задаем реальные размеры области доступной для печати и отступы которые требует принтер
				//todo - модифицировать flow document не очень хорошо тк мы им не владеем
				//todo - альбомный формат печати игнорируется
				var padding = flowDocument.PagePadding;
				try {
					var imageableArea = dialog.PrintQueue.GetPrintCapabilities().PageImageableArea;
					var documentPaginator = GetPaginator(flowDocument, doc.Value.Item2, dialog.PageRangeSelection, dialog.PageRange);
					var orientation = GetPageOrientation(documentPaginator);
					if (imageableArea != null) {
						if (imageableArea.OriginWidth > 0 || imageableArea.OriginHeight > 0)
							flowDocument.PagePadding = new Thickness(
								imageableArea.OriginWidth,
								imageableArea.OriginHeight,
								0,
								0);
						if (orientation == PageOrientation.Landscape)
							documentPaginator.PageSize = new Size(imageableArea.ExtentHeight, imageableArea.ExtentWidth);
						else
							documentPaginator.PageSize = new Size(imageableArea.ExtentWidth, imageableArea.ExtentHeight);
					}

					if (orientation != PageOrientation.Unknown)
						dialog.PrintTicket.PageOrientation = orientation;
					dialog.PrintDocument(documentPaginator, name);
				}
				finally {
					flowDocument.PagePadding = padding;
				}
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

		public DocumentPaginator Paginator
		{
			get
			{
				return GetPaginator(PageRangeSelection.AllPages, new PageRange());
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