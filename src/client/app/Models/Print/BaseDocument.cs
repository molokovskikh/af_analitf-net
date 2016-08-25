using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class PrintColumn
	{
		public PrintColumn(string name, int width, int colSpan = 1)
		{
			Name = name;
			Width = width;
			ColSpan = colSpan;
		}

		public int ColSpan;
		public string Name;
		public int Width;
	}

	public class DocumentTemplate
	{
		public List<FrameworkContentElement> Parts = new List<FrameworkContentElement>();

		public Block ToBlock()
		{
			var table = new Table {
				Columns = {
					new TableColumn {
						Width = GridLength.Auto
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
									new TableCell((Block)Parts[0]),
									new TableCell((Block)Parts[1])
								}
							}
						}
					}
				}
			};
			return table;
		}

		public bool IsReady
		{
			get { return Parts.Count == 2; }
		}
	}

	public class ColumnGroup
	{
		public string Name;
		public int First;
		public int Last;

		public ColumnGroup(string name, int first, int last)
		{
			Name = name;
			First = first;
			Last = last;
		}
	}

	public class DefaultDocument : BaseDocument
	{
		public DefaultDocument(FlowDocument doc)
		{
			this.doc = doc;
			Configure();
		}

		protected override void BuildDoc()
		{}
	}

	public abstract class BaseDocument
	{
		private bool builded;

		protected FlowDocument doc = new FlowDocument();
		protected Style BlockStyle;
		protected Style HeaderStyle;
		protected Style TableHeaderStyle;
		protected Style CellStyle;
		protected Style TableStyle;

		public BaseDocument()
		{
			doc.FontFamily = new FontFamily("Arial");
			Configure();
			HeaderStyle = new Style(typeof(Run)) {
				Setters = {
					new Setter(Control.FontSizeProperty, 16d),
					new Setter(Control.FontWeightProperty, FontWeights.Bold),
				}
			};
			TableHeaderStyle = new Style(typeof(TableCell)) {
				Setters = {
					new Setter(Control.FontWeightProperty, FontWeights.Bold),
					new Setter(TableCell.TextAlignmentProperty, TextAlignment.Center),
					new Setter(TableCell.BorderBrushProperty, Brushes.Black),
					new Setter(TableCell.BorderThicknessProperty, new Thickness(0, 0, 1, 1)),
					new Setter(TableCell.LineStackingStrategyProperty, LineStackingStrategy.MaxHeight),
					new Setter(TableCell.PaddingProperty, new Thickness(2, 1, 2, 1)),
				}
			};
			CellStyle = new Style(typeof(TableCell)) {
				Setters = {
					new Setter(TableCell.BorderBrushProperty, Brushes.Black),
					new Setter(TableCell.BorderThicknessProperty, new Thickness(0, 0, 1, 1)),
					new Setter(TableCell.PaddingProperty, new Thickness(2, 1, 2, 1)),
				}
			};
			TableStyle = new Style(typeof(Table)) {
				Setters = {
					new Setter(Table.FontSizeProperty, 13d),
					new Setter(Table.CellSpacingProperty, 0d),
					new Setter(Table.BorderBrushProperty, Brushes.Black),
					new Setter(Table.BorderThicknessProperty, new Thickness(1, 1, 0, 0)),
				}
			};
		}

		protected void Configure()
		{
			//отступы должны быть тк большинство принтеров требует их
			doc.PagePadding = new Thickness(36, 36, 50, 36);
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			//мы должны оставить место для "шапки" и "подвала"
			paginator.PageSize = new Size(paginator.PageSize.Width - WrapDocumentPaginator.Margins.Left - WrapDocumentPaginator.Margins.Right,
				paginator.PageSize.Height - WrapDocumentPaginator.Margins.Bottom - WrapDocumentPaginator.Margins.Top);
		}

		public FlowDocument Build()
		{
			if (!builded) {
				BuildDoc();
				builded = true;
			}

			return doc;
		}

		protected abstract void BuildDoc();

		public object Settings { get; protected set; }

		public virtual FrameworkContentElement GetHeader(int page, int pageCount)
		{
			//250 это примерный размер блока с датой, нужно молиться что бы хватило
			var pageSize = ((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize;
			var table = new Table {
				FontFamily = new FontFamily("Arial"),
				FontSize = 10,
				Columns = {
					new TableColumn {
						Width = new GridLength(pageSize.Width - 250)
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
									new TableCell(new Paragraph(new Run("Информационная поддержка АналитФармация 473-2606000")) {
										TextAlignment = TextAlignment.Left,
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
			return table;
		}

		public virtual FrameworkContentElement GetFooter(int page, int pageCount)
		{
			var footer = "Электронная почта: farm@analit.net, интернет: http://www.analit.net/";
			return new Paragraph(new Run(footer)) {
				FontFamily = new FontFamily("Arial"),
				FontSize = 10,
			};
		}

		protected void TwoColumnHeader(string leftHeader, string rightHeader)
		{
			var header = new Table {
				Columns = {
					new TableColumn {
						Width = new GridLength(520)
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
									new TableCell(new Paragraph(new Run(leftHeader)) {
										TextAlignment = TextAlignment.Left,
										FontWeight = FontWeights.Bold,
										FontSize = 16
									}),
									new TableCell(new Paragraph(new Run(rightHeader))) {
										TextAlignment = TextAlignment.Right
									}
								}
							}
						}
					}
				}
			};
			doc.Blocks.Add(header);
		}

		protected virtual Paragraph Block(string text)
		{
			var paragraph = new Paragraph(new Run(text)) {
				Style = BlockStyle
			};
			doc.Blocks.Add(paragraph);
			return paragraph;
		}

		public virtual Paragraph Header(string text)
		{
			var paragraph = new Paragraph(new Run(text) {
				Style = HeaderStyle
			});
			doc.Blocks.Add(paragraph);
			return paragraph;
		}

		protected Table BuildTable(IEnumerable<object[]> rows, PrintColumn[] columns, IEnumerable<ColumnGroup> groups = null)
		{
			var table = BuildTableHeader(columns, groups);
			BuildRows(rows, columns, table);
			doc.Blocks.Add(table);
			return table;
		}

		protected void BuildRows(IEnumerable<object[]> rows, PrintColumn[] headers, Table table)
		{
			var tableRowGroup = table.RowGroups[0];

			foreach (var data in rows) {
				BuildRow(headers, tableRowGroup, data);
			}
		}

		protected void BuildRow(PrintColumn[] headers, TableRowGroup tableRowGroup, object[] data)
		{
			var row = new TableRow();
			tableRowGroup.Rows.Add(row);

			for (var i = 0; i < data.Length; i++) {
				row.Cells.Add(Cell(data[i]));
			}
		}

		protected TableCell Cell(object value, int colspan = 0)
		{
			var text = "";
			if (value != null)
				text = value.ToString();

			//разделитель страницы может оказаться в середине ячейки
			var cell = new TableCell(new Paragraph(new Run(text)) {
				KeepTogether = true,
			});
			cell.Style = CellStyle;
			if (colspan > 0)
				cell.ColumnSpan = colspan;
			if (Util.IsNumeric(value)) {
				cell.TextAlignment = TextAlignment.Right;
			}
			else {
				cell.TextAlignment = TextAlignment.Left;
			}
			return cell;
		}

		protected Table BuildTableHeader(PrintColumn[] columns, IEnumerable<ColumnGroup> groups = null)
		{
			var table = new Table {
				Style = TableStyle,
			};

			foreach (var header in columns) {
				table.Columns.Add(new TableColumn {
					Width = new GridLength(header.Width)
				});
			}

			var tableRowGroup = new TableRowGroup();
			table.RowGroups.Add(tableRowGroup);

			groups = (groups ?? new ColumnGroup[0]);
			ColumnGroup lastgroup = null;
			var headerRow = new TableRow();
			for (var i = 0; i < columns.Length; i++) {
				var column = columns[i];
				if (String.IsNullOrEmpty(column.Name))
					continue;
				var group = groups.FirstOrDefault(g => i >= g.First && i <= g.Last);
				if (group != null && group == lastgroup)
					continue;

				lastgroup = group;
				var name = lastgroup != null ? lastgroup.Name : column.Name;
				var tableCell = new TableCell(new Paragraph(new Run(name))) {
					Style = TableHeaderStyle,
				};
				if (group != null) {
					tableCell.ColumnSpan = group.Last - group.First + 1;
				}
				else if (groups.Any()) {
					tableCell.RowSpan = 2;
				}
				if (column.ColSpan > 1) {
					tableCell.ColumnSpan = column.ColSpan;
				}

				headerRow.Cells.Add(tableCell);
			}
			tableRowGroup.Rows.Add(headerRow);

			if (groups.Any()) {
				var row = new TableRow();
				for (var i = 0; i < columns.Length; i++) {
					var column = columns[i];
					var name = column.Name;
					var group = groups.FirstOrDefault(g => i >= g.First && i <= g.Last);
					if (group == null)
						continue;

					var tableCell = new TableCell(new Paragraph(new Run(name))) {
						Style = TableHeaderStyle,
					};
					row.Cells.Add(tableCell);
				}
				tableRowGroup.Rows.Add(row);
			}

			return table;
		}

		protected void Landscape()
		{
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			var size = paginator.PageSize;
			paginator.PageSize = new Size(size.Height - WrapDocumentPaginator.Margins.Left - WrapDocumentPaginator.Margins.Right
					+ WrapDocumentPaginator.Margins.Top + WrapDocumentPaginator.Margins.Bottom,
				size.Width - WrapDocumentPaginator.Margins.Bottom - WrapDocumentPaginator.Margins.Top
					+ WrapDocumentPaginator.Margins.Left + WrapDocumentPaginator.Margins.Right);
		}
	}
}