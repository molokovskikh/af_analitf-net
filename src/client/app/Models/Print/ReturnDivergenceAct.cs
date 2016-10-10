using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class ReturnDivergenceAct : BaseDocument
	{
		private DocumentTemplate template;
		private ReturnToSupplier returnToSupplier;
		private WaybillSettings waybillSettings;
		private BlockUIContainer bodyBlock;

		public ReturnDivergenceAct(ReturnToSupplier returnToSupplier, WaybillSettings waybillSettings)
		{
			this.returnToSupplier = returnToSupplier;
			this.waybillSettings = waybillSettings;

			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			BlockStyle = new Style(typeof(Paragraph)) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
					new Setter(System.Windows.Documents.Block.MarginProperty, new Thickness(0, 3, 0, 3))
				}
			};

			HeaderStyle = new Style(typeof(Run), HeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 12d),
				}
			};
			TableHeaderStyle = new Style(typeof(TableCell), TableHeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
				}
			};
		}

		protected override void BuildDoc()
		{
			var right = Block("Унифицированная форма № Торг-12 \n" +
			                  "Утверждена постановлением Госкомстата \n" +
												"России от 25.12.98 № 132");
			right.TextAlignment = TextAlignment.Right;
			right.FontSize = 10;
			right.FontStyle = FontStyles.Italic;

			var header = new Grid();
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header
				.Cell(0, 0, LeftHeaderTable())
				.Cell(0, 1, RightHeaderTable())
				.Cell(1, 0, Caption())
				.Cell(1, 1, CaptionSignature());;
			header.ShowGridLines = true;
			doc.Blocks.Add(new BlockUIContainer(header));

			/*var caption = new Grid();
			caption
				.Cell(0, 0, Caption())
				.Cell(0, 1, CaptionSignature());
			doc.Blocks.Add(new BlockUIContainer(caption));*/
		}

		private void TwoColumns()
		{
			template = new DocumentTemplate();
		}

		private Grid LeftHeaderTable()
		{
			var grid = new Grid();
			grid.Cell(0, 0,
					SingBlockHeader(returnToSupplier.SupplierName,
						"организация, адрес, номер телефона, факса"))
				.Cell(1, 0, SingBlockHeader(waybillSettings.Address, "структурное подразделение"))
				.Cell(2, 0, SingBlockHeaderLabel("Основание для составления акта", "приказ, распоряжение"));
			grid.VerticalAlignment = VerticalAlignment.Center;
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
		}

		private Grid RightHeaderTable()
		{
			var grid = new Grid();
			grid
				.Cell(0, 0, LabelWithoutBorder(""))
				.Cell(0, 1, LabelWithBorder("Код"))
				.Cell(1, 0, LabelWithoutBorder("Форма по ОКУД"))
				.Cell(1, 1, LabelWithBorder("330202"))
				.Cell(2, 0, LabelWithoutBorder("по ОКПО"))
				.Cell(2, 1, LabelWithBorder(""))
				.Cell(3, 0, LabelWithoutBorder(""))
				.Cell(3, 1, LabelWithBorder(""))
				.Cell(4, 0, LabelWithoutBorder(""))
				.Cell(4, 1, LabelWithBorder(""))
				.Cell(5, 0, LabelWithoutBorder("Вид деятельности по ОКДП"))
				.Cell(5, 1, LabelWithBorder(""))
				.Cell(6, 0, LabelWithoutBorder("номер"))
				.Cell(6, 1, LabelWithBorder(""))
				.Cell(7, 0, LabelWithoutBorder("дата"))
				.Cell(7, 1, LabelWithBorder(""))
				.Cell(8, 0, LabelWithoutBorder("Вид операции"))
				.Cell(8, 1, LabelWithBorder(""));
			return grid;
		}

		private static Label LabelWithoutBorder(string text)
		{
			return new Label
			{
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap,
				},
				HorizontalContentAlignment = HorizontalAlignment.Right
			};
		}

		private static Label LabelWithBorder(string text)
		{
			return new Label
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1, 1, 1, 1),
				SnapsToDevicePixels = true,
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap,
					TextAlignment = TextAlignment.Center,
					Width = 180
				},
				HorizontalAlignment = HorizontalAlignment.Right
			};
		}

		private Grid SingBlockHeader(string text, string signature)
		{
			var grid = new Grid();
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
				Content = text,
				Width = 500,
				FontWeight = FontWeights.Bold,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = signature,
				Width = 500,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
		}

		private Grid SingBlockHeaderLabel(string label, string text)
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			grid.Cell(0, 0, new Label {
				Content = label,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
				Content = text,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.HorizontalAlignment = HorizontalAlignment.Left;
			return grid;
		}

		private static Grid SingBlock(string text, string signature)
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});

			grid.Cell(0, 0, new Label {
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap
				},
			});
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private Grid Caption()
		{
			var grid = new Grid();
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			grid
			.Cell(0, 0, CaptionTable())
			.Cell(1, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "об установленном расхождении по количеству",
						HorizontalAlignment = HorizontalAlignment.Center
					})
			.Cell(2, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "и качеству при приемке товарно-материальных ценностей",
						HorizontalAlignment = HorizontalAlignment.Center
					});
			return grid;
		}

		private Grid CaptionSignature()
		{
			var grid = new Grid();
			grid.Cell(0, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Content = "УТВЕРЖДАЮ",
						HorizontalAlignment = HorizontalAlignment.Center
					})
				.Cell(1, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						FontWeight = FontWeights.Bold,
						Content = "Руководитель",
						HorizontalAlignment = HorizontalAlignment.Center
					})
				.Cell(2, 0, SingBlock("", "(должность)"))
				.Cell(3, 0, new Grid()
					.Cell(0, 0, SingBlock("", "(подпись)"))
					.Cell(0, 1, SingBlock("", "  (расшифровка подписи)  ")))
				.Cell(4, 0, new Grid()
					.Cell(0, 0, LabelWithoutBorder("<<______>>"))
					.Cell(0, 1, LabelWithoutBorder("__________ _____года"))
				);
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}
		private Grid CaptionTable()
		{
			var grid = new Grid().Cell(1, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 14,
					FontWeight = FontWeights.Bold,
					SnapsToDevicePixels = true,
					Content = "АКТ",
					HorizontalAlignment = HorizontalAlignment.Center
				})
				.Cell(0, 1, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = "Номер документа"
				})
				.Cell(0, 2, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = "Дата составления"
				})
				.Cell(1, 1, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = "В1-89"
				})
				.Cell(1, 2, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = DateTime.Now.ToString("dd/M/yyyy")
				})
				.Cell(0, 0, new Label
				{
					SnapsToDevicePixels = true,
				});
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
		}

		protected override Paragraph Block(string text)
		{
			var block = base.Block(text);
			CheckTemplate(block);
			return block;
		}

		public override Paragraph Header(string text)
		{
			var block = base.Header(text);
			CheckTemplate(block);
			return block;
		}

		private void CheckTemplate(Paragraph block)
		{
			if (template != null) {
				doc.Blocks.Remove(block);
				Stash(block);
			}
		}

		private void Stash(FrameworkContentElement element)
		{
			template.Parts.Add(element);
			if (template.IsReady) {
				doc.Blocks.Add(template.ToBlock());
				template = null;
			}
		}

		private void BodyLine(string label, string value)
		{
			if (bodyBlock == null) {
				bodyBlock = new BlockUIContainer();
				bodyBlock.Child = new Grid {
					HorizontalAlignment = HorizontalAlignment.Left,
					Margin = new Thickness(0, 10, 0, 10),
					Width = 820,
					ColumnDefinitions = {
						new ColumnDefinition(),
						new ColumnDefinition {
							Width = GridLength.Auto
						}
					}
				};
				doc.Blocks.Add(bodyBlock);
			}
			var grid = (Grid)bodyBlock.Child;
			grid.RowDefinitions.Add(new RowDefinition());
			var inner = new Grid();
			inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			inner.ColumnDefinitions.Add(new ColumnDefinition());
			var labelEl = new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8,
				Content = label,
			};
			labelEl.SetValue(Grid.ColumnProperty, 0);
			var valueEl = new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Content = value,
			};
			valueEl.SetValue(Grid.ColumnProperty, 1);
			inner.Children.Add(labelEl);
			inner.Children.Add(valueEl);
			inner.SetValue(Grid.ColumnProperty, 0);
			inner.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
			grid.Children.Add(inner);
		}
	}
}
