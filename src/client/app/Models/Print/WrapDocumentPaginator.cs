using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Print
{
	public class WrapDocumentPaginator : DocumentPaginator, IDocumentPaginatorSource
	{
		public static Thickness Margins = new Thickness(0, 50, 0, 50);

		private BaseDocument document;
		private DocumentPaginator paginator;
		private Size pageSize;
		private PageRangeSelection selection;
		private PageRange range;

		public WrapDocumentPaginator(FlowDocument flowDoc, BaseDocument baseDoc,
			PageRangeSelection selection = PageRangeSelection.AllPages,
			PageRange range = default(PageRange))
			: this(((IDocumentPaginatorSource)flowDoc).DocumentPaginator, baseDoc)
		{
			this.selection = selection;
			this.range = range;
		}

		public WrapDocumentPaginator(DocumentPaginator paginator, BaseDocument document)
		{
			this.document = document;
			this.paginator = paginator;

			//будь бдителен, это очистка кеша
			//DocumentPaginator содержит внутри себя кеш страниц которые он сформировал
			//но если мы сформируем два WrapDocumentPaginator то при попытке сформировать страницу будет ошибка
			//System.ArgumentException : Указанный элемент Visual уже является дочерним по отношению к другому элементу Visual или корневому элементу CompositionTarget.
			//эта ошибка вызвана тем что страница из кеша присваивается как дочерняя двум результирующим страницам
			//что бы этого избежать при создании очищаем кеш
			var size = paginator.PageSize;
			paginator.PageSize = new Size(0, 0);
			paginator.PageSize = size;

			pageSize = new Size(paginator.PageSize.Width + Margins.Right + Margins.Left,
				paginator.PageSize.Height + Margins.Top + Margins.Bottom);
		}

		public override DocumentPage GetPage(int pageNumber)
		{
			//что бы paginator.PageCount вернул корректное значение нужно сформировать все страницы
			if (!paginator.IsPageCountValid) {
				var totalPages = 0;
				while (paginator.GetPage(totalPages++) != DocumentPage.Missing) {}
			}
			if (selection == PageRangeSelection.UserPages){
				pageNumber = pageNumber + Math.Min(range.PageFrom, paginator.PageCount) - 1;
			}

			var originalVisual = paginator.GetPage(pageNumber).Visual;

			var visual = new ContainerVisual();
			var pageVisual = new ContainerVisual {
				Transform = new TranslateTransform(
					Margins.Left,
					Margins.Top)
			};
			pageVisual.Children.Add(originalVisual);
			visual.Children.Add(pageVisual);

			var header = Header(pageNumber);
			if (header != null) {
				var headerContainer = new ContainerVisual {
					Transform = new TranslateTransform(Margins.Left, 0)
				};
				headerContainer.Children.Add(header);
				visual.Children.Add(headerContainer);
			}

			var footer = Footer(pageNumber);
			if (footer != null) {
				var footerContainer = new ContainerVisual {
					Transform = new TranslateTransform(Margins.Left, PageSize.Height - Margins.Bottom)
				};
				footerContainer.Children.Add(footer);
				visual.Children.Add(footerContainer);
			}

			var documentPage = new DocumentPage(visual,
				pageSize,
				new Rect(new Point(), pageSize),
				new Rect(new Point(Margins.Left, Margins.Top), ContentSize()));
			return documentPage;
		}

		private Visual Clone(Visual visual)
		{
			var memory = new MemoryStream();
			var s = XamlWriter.Save(visual);
			return (Visual)XamlReader.Parse(s);
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
			return new Size(pageSize.Width - Margins.Left - Margins.Right,
				pageSize.Height - Margins.Top - Margins.Bottom);
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

		public override bool IsPageCountValid => paginator.IsPageCountValid;

		public override int PageCount
		{
			get
			{
				if (selection == PageRangeSelection.UserPages) {
					var max = paginator.PageCount;
					if (max == 0)
						return 0;
					return Math.Min(range.PageTo, max) - Math.Min(max, range.PageFrom) + 1;
				}
				return paginator.PageCount;
			}
		}

		public override Size PageSize
		{
			get
			{
				return pageSize;
			}
			set
			{
				//операцию обновления размера невозможно реализовать корректно
				//размер сраницы должен быть на 50 сверху и на 50 снизу меньше чем размер a4 что бы уместились подписи
				//но если мы сконструируем 2 WrapDocumentPaginator каждый из них уменьшит размер страницы на 100
				//в результате страница странет меньше на 200px
				//по этому мы предполагаем что документ который поступает на вход уже содержит необходимые отступы
				throw new NotImplementedException("Эту операцию невозможно реализовать корректно");
			}
		}

		public override IDocumentPaginatorSource Source => paginator.Source;

		public DocumentPaginator DocumentPaginator => this;
	}
}