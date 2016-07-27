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
			doc.Blocks.Add(new BlockUIContainer(new Grid()
				.Cell(0, 0, new Grid()
					.Cell(0, 0, SignatureBlock("", "(организация, номер телефона)"))
				)
				.Cell(0, 1, RightHeaderTable(0)
				)
			));

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

			BuildTable(rows, headers);

			var tax10Sum = waybill.Lines.Where(l => l.Nds == 10).Select(l => l.NdsAmount).Sum();
			var tax10Block = Block(string.Format("Итого НДС 10%: {0:0.00} руб", tax10Sum));
			tax10Block.FontWeight = FontWeights.Bold;

			var tax18Sum = waybill.Lines.Where(l => l.Nds == 18).Select(l => l.NdsAmount).Sum();
			var tax18Block = Block(string.Format("Итого НДС 18%: {0:0.00} руб", tax18Sum));
			tax18Block.FontWeight = FontWeights.Bold;

			doc.Blocks.Add(new BlockUIContainer(new Grid()
				.Cell(0, 0, SingBlock("Руководитель организации\r\nили иное уполномоченное лицо"))
				.Cell(1, 0, SingBlock("Индивидуальный предприниматель"))
				.RowSpan(0, 1, 2, new Label {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Content = "М. П.",
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				})
				.Cell(0, 2, SingBlock("Главный бухгалтер\r\nили иное уполномоченное лицо"))
				.Cell(1, 2, new Grid()
					.Cell(0, 0, new Label {
						Width = 430,
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(0, 0, 0, 1),
						SnapsToDevicePixels = true,
					})
					.Cell(1, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 9,
						Content = new TextBlock {
							Text = "(реквизиты свидетельства о государственной регистрации\r\nиндивидуального предпринимателя)",
							TextAlignment = TextAlignment.Center,
							TextWrapping = TextWrapping.Wrap
						},
						HorizontalAlignment = HorizontalAlignment.Center,
					}))
			));
		}

		private static Grid SignatureBlock(string text, string signature)
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
				Width = GridLength.Auto
			});

			grid.RowSpan(0, 0, 2, new Label {
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
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private static Grid RightHeaderTable(int code)
		{
			var grid = new Grid()
				.Cell(0, 0, CellWithoutBorder(""))
				.Cell(0, 1, CellWithBorder("Код"))
				.Cell(1, 0, CellWithoutBorder("Форма по ОКУД"))
				.Cell(1, 1, CellWithBorder(code.ToString()))
				.Cell(2, 0, CellWithoutBorder("по ОКПО"))
				.Cell(2, 1, CellWithBorder(""))
				.Cell(3, 0, CellWithoutBorder("ИНН"))
				.Cell(3, 1, CellWithBorder(""))
				.Cell(4, 0, CellWithoutBorder(""))
				.Cell(4, 1, CellWithBorder(""))
				.Cell(5, 0, CellWithoutBorder("Вид деятельности по ОКДП"))
				.Cell(5, 1, CellWithBorder(""))
				.Cell(6, 0, CellWithoutBorder("Номер производителя"))
				.Cell(6, 1, CellWithBorder(""))
				.Cell(7, 0, CellWithoutBorder("Номер регистрационный"))
				.Cell(7, 1, CellWithBorder(""))
				.Cell(8, 0, CellWithoutBorder(""))
				.Cell(8, 1, CellWithBorder(""))
				.Cell(9, 0, CellWithoutBorder("Кассир"))
				.Cell(9, 1, CellWithBorder(""))
				.Cell(10, 0, CellWithoutBorder("Вид операции"))
				.Cell(10, 1, CellWithBorder(""));
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}

		private static Label CellWithoutBorder(string text)
		{
			return new Label
			{
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap
				},
			};
		}
		private static Label CellWithBorder(string text)
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
					TextWrapping = TextWrapping.Wrap
				},
			};
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
				Width = GridLength.Auto
			});

			grid.RowSpan(0, 0, 2, new Label {
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = name,
					TextWrapping = TextWrapping.Wrap
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
				Width = 145,
				BorderBrush = Brushes.Black,
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
