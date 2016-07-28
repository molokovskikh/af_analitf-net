using AnalitF.Net.Client.Models.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using Common.Tools;


namespace AnalitF.Net.Client.Models.Print
{
	class ReturnActDocument : BaseDocument
	{
		private Check[] _checks;
		private BlockUIContainer headerBlock;

		public ReturnActDocument(Check[] checks)
		{
			_checks = checks;
		}

		protected override void BuildDoc()
		{
			var waybill= new Waybill();
			/*
			var headerTable = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				Children = {
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = string.Format("СЧЕТ-ФАКТУРА № {0} от {1:d}", waybill.InvoiceId, waybill.InvoiceDate)
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "(1)"
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "ИСПРАВЛЕНИЕ № __________ от \" ___ \" ______________"
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "(1а)",
					}
				}
			};
			headerTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			headerTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			headerTable.RowDefinitions.Add(new RowDefinition());
			headerTable.RowDefinitions.Add(new RowDefinition());

			headerTable.Children[0].SetValue(Grid.RowProperty, 0);
			headerTable.Children[0].SetValue(Grid.ColumnProperty, 0);

			headerTable.Children[1].SetValue(Grid.RowProperty, 0);
			headerTable.Children[1].SetValue(Grid.ColumnProperty, 1);

			headerTable.Children[2].SetValue(Grid.RowProperty, 1);
			headerTable.Children[2].SetValue(Grid.ColumnProperty, 0);

			headerTable.Children[3].SetValue(Grid.RowProperty, 1);
			headerTable.Children[3].SetValue(Grid.ColumnProperty, 1);

			doc.Blocks.Add(new BlockUIContainer(headerTable));

			

			HeaderLine("Продавец", waybill.Seller == null ? "" : waybill.Seller.Name, "2");
			HeaderLine("Адрес продавца", waybill.Seller == null ? "" : waybill.Seller.Address, "2а");
			HeaderLine("ИНН/КПП", String.Format("{0}/{1}", waybill.Seller == null ? "" : waybill.Seller.Inn, waybill.Seller == null ? "" : waybill.Seller.Kpp), "2б");
			HeaderLine("Грузоотправитель и его адрес", waybill.ShipperNameAndAddress, "3");
			HeaderLine("Грузополучатель и его адрес", waybill.ConsigneeNameAndAddress, "4");
			HeaderLine("К платежно-расчетному документу №_______________ от _______________", "", "5");
			HeaderLine("Покупатель", waybill.Buyer == null ? "" : waybill.Buyer.Name, "6");
			HeaderLine("Адрес покупателя", waybill.Buyer == null ? "" : waybill.Buyer.Address, "6а");
			HeaderLine("ИНН/КПП покупателя", String.Format("{0}/{1}", waybill.Buyer == null ? "" : waybill.Buyer.Inn, waybill.Buyer == null ? "" : waybill.Buyer.Kpp), "6б");
			HeaderLine("Валюта: наименование, код", "российский рубль, код 643", "7");
			*/
			
			doc.Blocks.Add(new BlockUIContainer(HeaderTable(0)));
			doc.Blocks.Add(new BlockUIContainer(new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = "Настоящий акт составлен комиссией, которая установила:"
			}));

			var headers = new[]
			{
				new PrintColumn("№ по порядку", 60),
				new PrintColumn("Отдел", 90),
				new PrintColumn("Код бригады", 90),
				new PrintColumn("№ чека", 120),
				new PrintColumn("Сумма с уч. скидки", 80),
				new PrintColumn("Должность, фамилия, и.о., лица, разрешившего возврат денег по чеку", 180),
			};

			var rows = _checks.Select((o, i) => new object[]
			{
				o.Id,
				o.Department,
				o.ChangeNumber,
				o.Number,
				o.RetailSum,
				o.Agent,
			});

			var table = BuildTable(rows, headers);
			var TotalSum = _checks.Sum(l => l.RetailSum);
			if (_checks.Length > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run(TotalSum.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						}
					}
				});
			}
			/*doc.Blocks.Add(new BlockUIContainer(SingBlock("Выдано покупателям (клиентам) по возвращенным ими кассовым чекам (по ошибочно пробитым чекам) согласно акту на сумму:"
				,"(прописью)", 500)));
			doc.Blocks.Add(new BlockUIContainer(new Label
			{
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap
				},
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = "На указанную сумму следует уменьшить выручку кассы."
			}));
			doc.Blocks.Add(
				new BlockUIContainer(
					SingBlock(
						"Перечисленные возвращенные покупателями (клиентами) чеки (ошибочно пробитые чеки) погашены и прилогаются к акту. Приложение:",
						"", 500)));
						*/
			doc.Blocks.Add(new BlockUIContainer(new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = "Выдано покупателям (клиентам) по возвращенным ими кассовым чекам (по ошибочно пробитым чекам) " +
					       "согласно акту на сумму:_______________________________________________________________________",
					TextWrapping = TextWrapping.Wrap
				}
			}));
			doc.Blocks.Add(new BlockUIContainer(new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = "На указанную сумму следует уменьшить выручку кассы."
			}));
			doc.Blocks.Add(new BlockUIContainer(new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = "Перечисленные возвращенные покупателями (клиентами) чеки (ошибочно пробитые чеки) погашены и прилогаются к акту. Приложение:" +
					       "_____________________________________",
					TextWrapping = TextWrapping.Wrap
				}
			}));
			doc.Blocks.Add(new BlockUIContainer(new Grid()
				.Cell(0, 0, SingBlock("Заведующий отделом (секцией)"))
				.Cell(1, 0, SingBlock("Старший кассир"))
				.Cell(2, 0, SingBlock("Кассир операционист"))
				.Cell(3, 0, SingBlock())
				.Cell(4, 0, SingBlock())
			));
		}

		

		private static Grid HeaderTable(int code)
		{
			var header = new Grid();
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header.Cell(0, 0, new Grid()
					.Cell(0, 0, SingBlock("", "(организация, номер телефона)"))
					.Cell(1, 0, SingBlock("", ""))
					.Cell(2, 0, SingBlock("", "(структурное подразделение)"))
					.Cell(3, 0, SingBlock("Контрольно-кассовая машина", "(модель(класс, тип, марка))", 300))
					.Cell(4, 0, SingBlock("Прикладная программа", "(наименование)", 300))
				)
				.Cell(0, 1, RightHeaderTable(code)
				)
				.Cell(1, 0, Header())
				.Cell(1, 1, Caption()
				);
			return header;
		}
		private static Grid Header()
		{
			var grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				Children = {
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "ВОЗВРАТЕ ДЕНЕЖНЫХ СУММ ПОКУПАТЕЛЯМ (КЛИЕНТАМ)",
						HorizontalAlignment = HorizontalAlignment.Center
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "ПО НЕИСПОЛЬЗОВАННЫМ КАССОВЫМ ЧЕКАМ",
						HorizontalAlignment = HorizontalAlignment.Center
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "(в том числе по ошибочно пробитым чекам)",
						HorizontalAlignment = HorizontalAlignment.Center
					}
				}
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());

			grid.Cell(0, 0, Head());

			grid.Children[0].SetValue(Grid.RowProperty, 1);
			grid.Children[0].SetValue(Grid.ColumnProperty, 0);

			grid.Children[1].SetValue(Grid.RowProperty, 2);
			grid.Children[1].SetValue(Grid.ColumnProperty, 0);

			grid.Children[2].SetValue(Grid.RowProperty, 3);
			grid.Children[2].SetValue(Grid.ColumnProperty, 0);

			grid.Children[3].SetValue(Grid.RowProperty, 0);
			grid.Children[3].SetValue(Grid.ColumnProperty, 0);
			return grid;
		}
		private static Grid Caption()
		{
			var grid = new Grid();
			grid.Cell(0, 0, LabelWithoutBorder("УТВЕРЖДАЮ"))
				.Cell(1, 0, LabelWithoutBorder("Руководитель"))
				.Cell(2, 0, SingBlock("", "(должность)"))
				.Cell(3, 0, new Grid()
					.Cell(0, 0, SingBlock("", "(подпись)"))
					.Cell(0, 1, SingBlock("", "  (расшифровка подписи)  ")))
				.Cell(4, 0, new Grid()
					.Cell(0, 0, LabelWithoutBorder("<<______>>"))
					.Cell(0, 1, LabelWithoutBorder("__________ _____г."))
				);
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}
		private static Grid Head()
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
					Content = ""
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
		private static Grid RightHeaderTable(int code)
		{
			var grid = new Grid()
				.Cell(0, 0, LabelWithoutBorder(""))
				.Cell(0, 1, LabelWithBorder("Код"))
				.Cell(1, 0, LabelWithoutBorder("Форма по ОКУД"))
				.Cell(1, 1, LabelWithBorder(code.ToString()))
				.Cell(2, 0, LabelWithoutBorder("по ОКПО"))
				.Cell(2, 1, LabelWithBorder(""))
				.Cell(3, 0, LabelWithoutBorder("ИНН"))
				.Cell(3, 1, LabelWithBorder(""))
				.Cell(4, 0, LabelWithoutBorder(""))
				.Cell(4, 1, LabelWithBorder(""))
				.Cell(5, 0, LabelWithoutBorder("Вид деятельности по ОКДП"))
				.Cell(5, 1, LabelWithBorder(""))
				.Cell(6, 0, LabelWithoutBorder("Номер производителя"))
				.Cell(6, 1, LabelWithBorder(""))
				.Cell(7, 0, LabelWithoutBorder("Номер регистрационный"))
				.Cell(7, 1, LabelWithBorder(""))
				.Cell(8, 0, LabelWithoutBorder(""))
				.Cell(8, 1, LabelWithBorder(""))
				.Cell(9, 0, LabelWithoutBorder("Кассир"))
				.Cell(9, 1, LabelWithBorder(""))
				.Cell(10, 0, LabelWithoutBorder("Вид операции"))
				.Cell(10, 1, LabelWithBorder(""));
			grid.HorizontalAlignment = HorizontalAlignment.Right;
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
					Width = 90,
					TextAlignment = TextAlignment.Center
				},
			};
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

		private static Grid SingBlock(string text, string signature, int size)
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
				Width = size,
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private static Grid SingBlock(string name)
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
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
					Text = name,
					TextWrapping = TextWrapping.Wrap,
					Width = 200
				},
			});

			grid.Cell(0, 1, new Label {
				Width = 87,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(подпись)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			grid.Cell(0, 2, new Label {
				
				BorderBrush = Brushes.Black,
				Width = 350,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 2, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(ф.и.о.)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private static Grid SingBlock()
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			grid.Cell(0, 0, new Label {
				Width = 200,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 0, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(должность)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			grid.Cell(0, 1, new Label {
				Width = 87,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(подпись)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			grid.Cell(0, 2, new Label {
				BorderBrush = Brushes.Black,
				Width = 350,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 2, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(ф.и.о.)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private void HeaderLine(string label, string value, string id)
		{
			if (headerBlock == null) {
				headerBlock = new BlockUIContainer();
				headerBlock.Child = new Grid {
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
				doc.Blocks.Add(headerBlock);
			}
			var grid = (Grid)headerBlock.Child;
			grid.RowDefinitions.Add(new RowDefinition());
			var inner = new Grid();
			inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			inner.ColumnDefinitions.Add(new ColumnDefinition());
			var labelEl = new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = label,
			};
			labelEl.SetValue(Grid.ColumnProperty, 0);
			var valueEl = new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
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
			var idEl = new Label {
				Content = "(" + id + ")",
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
			};
			idEl.SetValue(Grid.ColumnProperty, 1);
			idEl.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
			grid.Children.Add(idEl);
		}
	}
}
