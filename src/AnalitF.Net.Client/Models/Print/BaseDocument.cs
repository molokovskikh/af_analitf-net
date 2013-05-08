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
	public class DefaultDocument : BaseDocument
	{
		public FlowDocument Document
		{
			get { return doc; }
			set { doc = value; }
		}

		public override FlowDocument Build()
		{
			return doc;
		}
	}

	public abstract class BaseDocument
	{
		protected FlowDocument doc = new FlowDocument();
		protected Style BlockStyle;
		protected Style HeaderStyle;
		protected Style TableHeaderStyle;
		protected Style CellStyle;
		protected Style TableStyle;

		public BaseDocument()
		{
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
					new Setter(TableCell.PaddingProperty, new Thickness(2, 0, 2, 0)),
				}
			};
			CellStyle = new Style(typeof(TableCell)) {
				Setters = {
					new Setter(TableCell.BorderBrushProperty, Brushes.Black),
					new Setter(TableCell.BorderThicknessProperty, new Thickness(0, 0, 1, 1)),
					new Setter(TableCell.PaddingProperty, new Thickness(2, 0, 2, 0)),
				}
			};
			TableStyle = new Style(typeof(Table)) {
				Setters = {
					new Setter(Table.FontSizeProperty, 10d),
					new Setter(Table.CellSpacingProperty, 0d),
					new Setter(Table.BorderBrushProperty, Brushes.Black),
					new Setter(Table.BorderThicknessProperty, new Thickness(1, 1, 0, 0)),
				}
			};
		}

		public abstract FlowDocument Build();

		public virtual FrameworkContentElement GetHeader(int page, int pageCount)
		{
			//250 это примерный размер блока с датой, нужно молиться что бы хватило
			var pageSize = ((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize;
			var table = new Table {
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
			return table;
		}

		public virtual FrameworkContentElement GetFooter(int page, int pageCount)
		{
			var footer = "Электронная почта: farm@analit.net, интернет: http://www.analit.net/";
			return new Paragraph(new Run(footer));
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

		protected Table BuildTable(IEnumerable<object[]> rows, PrintColumnDeclaration[] columns, IEnumerable<ColumnGroup> groups = null)
		{
			var table = BuildTableHeader(columns, groups);
			BuildRows(rows, columns, table);
			doc.Blocks.Add(table);
			return table;
		}

		protected void BuildRows(IEnumerable<object[]> rows, PrintColumnDeclaration[] headers, Table table)
		{
			var tableRowGroup = table.RowGroups[0];

			foreach (var data in rows) {
				BuildRow(headers, tableRowGroup, data);
			}
		}

		protected void BuildRow(PrintColumnDeclaration[] headers, TableRowGroup tableRowGroup, object[] data)
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

			var cell = new TableCell(new Paragraph(new Run(text)));
			cell.Style = CellStyle;
			if (colspan > 0)
				cell.ColumnSpan = colspan;
			if (Util.IsDigitValue(value)) {
				cell.TextAlignment = TextAlignment.Right;
			}
			return cell;
		}

		protected Table BuildTableHeader(PrintColumnDeclaration[] columns, IEnumerable<ColumnGroup> groups = null)
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

			var headerRow = new TableRow();
			for (var i = 0; i < columns.Length; i++) {
				var column = columns[i];
				var group = groups.FirstOrDefault(g => g.First == i);
				var name = group != null ? group.Name : column.Name;
				var tableCell = new TableCell(new Paragraph(new Run(name))) {
					Style = TableHeaderStyle
				};

				headerRow.Cells.Add(tableCell);
			}
			tableRowGroup.Rows.Add(headerRow);
			return table;
		}

		protected void Landscape()
		{
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			var size = paginator.PageSize;
			paginator.PageSize = new Size(size.Height, size.Width);
		}
	}
}