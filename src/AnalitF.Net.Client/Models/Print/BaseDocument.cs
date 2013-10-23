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
		public PrintColumn(string name, int width)
		{
			Name = name;
			Width = width;
		}

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

			var cell = new TableCell(new Paragraph(new Run(text)));
			cell.Style = CellStyle;
			if (colspan > 0)
				cell.ColumnSpan = colspan;
			if (Util.IsDigitValue(value)) {
				cell.TextAlignment = TextAlignment.Right;
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
			paginator.PageSize = new Size(size.Height, size.Width);
		}
	}
}