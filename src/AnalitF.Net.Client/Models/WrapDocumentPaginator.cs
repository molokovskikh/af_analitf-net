using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models
{
	public class WrapDocumentPaginator : DocumentPaginator, IDocumentPaginatorSource
	{
		private DocumentPaginator _paginator;
		private Size _pageSize;
		private Thickness _margins;

		public WrapDocumentPaginator(DocumentPaginator paginator)
		{
			_margins = new Thickness(0, 50, 0, 50);
			_paginator = paginator;
			PageSize = paginator.PageSize;
		}

		public override DocumentPage GetPage(int pageNumber)
		{
			var originalVisual = _paginator.GetPage(pageNumber).Visual;

			var visual = new ContainerVisual();
			var pageVisual = new ContainerVisual {
				Transform = new TranslateTransform(
					_margins.Left,
					_margins.Top)
			};
			pageVisual.Children.Add(originalVisual);
			visual.Children.Add(pageVisual);

			var header = Header(pageNumber);
			if (header != null) {
				visual.Children.Add(header);
			}

			var footer = Footer(pageNumber);
			if (footer != null) {
				var footerContainer = new ContainerVisual {
					Transform = new TranslateTransform(_margins.Left, PageSize.Height - _margins.Bottom)
				};
				footerContainer.Children.Add(footer);
				visual.Children.Add(footerContainer);
			}

			return new DocumentPage(visual,
				_pageSize,
				new Rect(new Point(), _pageSize),
				new Rect(new Point(_margins.Left, _margins.Top), ContentSize()));
		}

		private Visual Header(int pageNumber)
		{
			var table = new Table {
				Columns = {
					new TableColumn {
						Width = new GridLength(560)
					},
					new TableColumn {
						Width = GridLength.Auto
					},
				},
				RowGroups = {
					new TableRowGroup {
						Rows = {
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run("Информационная поддержка \"АК \"Инфорум\"\" 473-2606000")) {
										TextAlignment = TextAlignment.Left,
										FontWeight = FontWeights.Bold,
										FontSize = 16
									}),
									new TableCell(new Paragraph(new Run(DateTime.Now.ToString()))) {
										TextAlignment = TextAlignment.Right
									}
								}
							}
						}
					}
				}
			};

			return ToVisual(table);
		}

		private Size ContentSize()
		{
			return new Size(_pageSize.Width - _margins.Left - _margins.Right,
				_pageSize.Height - _margins.Top - _margins.Bottom);
		}

		private Visual Footer(int pageNumber)
		{
			return ToVisual(new Paragraph(new Run("Электронная почта: farm@analit.net, интернет: http://www.analit.net/")));
		}

		private Visual ToVisual(Block section)
		{
			var doc = new FlowDocument(section) {
				ColumnGap = 0,
				ColumnWidth = double.PositiveInfinity
			};
			var document = _paginator.Source as FlowDocument;
			if (document != null) {
				doc.PagePadding = document.PagePadding;
			}
			var page = ((IDocumentPaginatorSource)doc).DocumentPaginator.GetPage(0);
			return page.Visual;
		}

		public override bool IsPageCountValid
		{
			get { return _paginator.IsPageCountValid; }
		}

		public override int PageCount
		{
			get { return _paginator.PageCount; }
		}

		public override Size PageSize
		{
			get
			{
				return _pageSize;
			}
			set
			{
				_pageSize = value;
				_paginator.PageSize = ContentSize();
			}
		}

		public override IDocumentPaginatorSource Source
		{
			get
			{
				return _paginator.Source;
			}
		}

		public DocumentPaginator DocumentPaginator
		{
			get { return this; }
		}
	}
}