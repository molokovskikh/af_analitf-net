using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class WrapDocumentPaginator : DocumentPaginator, IDocumentPaginatorSource
	{
		private BaseDocument document;
		private DocumentPaginator paginator;
		private Size pageSize;
		private Thickness margins;

		public WrapDocumentPaginator(DocumentPaginator paginator, BaseDocument document)
		{
			this.document = document;
			this.paginator = paginator;

			margins = new Thickness(0, 50, 0, 50);
			PageSize = paginator.PageSize;
		}

		public override DocumentPage GetPage(int pageNumber)
		{
			//что бы paginator.PageCount вернул корректное значение нужно сформировать все страницы
			var totalPages = 0;
			while (paginator.GetPage(totalPages++) != DocumentPage.Missing) {}

			var originalVisual = paginator.GetPage(pageNumber).Visual;

			var visual = new ContainerVisual();
			var pageVisual = new ContainerVisual {
				Transform = new TranslateTransform(
					margins.Left,
					margins.Top)
			};
			pageVisual.Children.Add(originalVisual);
			visual.Children.Add(pageVisual);

			var header = Header(pageNumber);
			if (header != null) {
				var headerContainer = new ContainerVisual {
					Transform = new TranslateTransform(margins.Left, 0)
				};
				headerContainer.Children.Add(header);
				visual.Children.Add(headerContainer);
			}

			var footer = Footer(pageNumber);
			if (footer != null) {
				var footerContainer = new ContainerVisual {
					Transform = new TranslateTransform(margins.Left, PageSize.Height - margins.Bottom)
				};
				footerContainer.Children.Add(footer);
				visual.Children.Add(footerContainer);
			}

			var documentPage = new DocumentPage(visual,
				pageSize,
				new Rect(new Point(), pageSize),
				new Rect(new Point(margins.Left, margins.Top), ContentSize()));
			return documentPage;
		}

		private Visual Header(int pageNumber)
		{
			if (document != null)
				return ToVisual(document.GetHeader(pageNumber, PageCount));
			return null;
		}

		private Visual Footer(int pageNumber)
		{
			if (document != null)
				return ToVisual(document.GetFooter(pageNumber, PageCount));
			return null;
		}

		private Size ContentSize()
		{
			return new Size(pageSize.Width - margins.Left - margins.Right,
				pageSize.Height - margins.Top - margins.Bottom);
		}

		private Visual ToVisual(FrameworkContentElement section)
		{
			if (section == null)
				return null;
			var doc = new FlowDocument((Block)section) {
				ColumnGap = 0,
				ColumnWidth = double.PositiveInfinity
			};
			var document = this.paginator.Source as FlowDocument;
			if (document != null) {
				doc.PagePadding = document.PagePadding;
			}
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			paginator.PageSize = ContentSize();
			var page = paginator.GetPage(0);
			return page.Visual;
		}

		public override bool IsPageCountValid
		{
			get { return paginator.IsPageCountValid; }
		}

		public override int PageCount
		{
			get { return paginator.PageCount; }
		}

		public override Size PageSize
		{
			get
			{
				return pageSize;
			}
			set
			{
				pageSize = value;
				paginator.PageSize = ContentSize();
			}
		}

		public override IDocumentPaginatorSource Source
		{
			get
			{
				return paginator.Source;
			}
		}

		public DocumentPaginator DocumentPaginator
		{
			get { return this; }
		}
	}
}