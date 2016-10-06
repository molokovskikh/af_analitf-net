using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	public class ReturnLabel : BaseDocument
	{
		private ReturnToSupplier returnToSupplier;
		public ReturnLabel(ReturnToSupplier _returnToSupplier)
		{
			returnToSupplier = _returnToSupplier;
		}

		protected override void BuildDoc()
		{
			var header = new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 24,
						Text = "ВОЗВРАТ",
						TextAlignment = TextAlignment.Center,
					},
					HorizontalAlignment = HorizontalAlignment.Center
				};
			header.HorizontalAlignment = HorizontalAlignment.Center;
			var body = new Grid();
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Auto)
			});
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			body.RowDefinitions.Add(new RowDefinition());
			body.RowDefinitions.Add(new RowDefinition());
			body.Cell(0, 0, new Grid()
				.Cell(0, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Text = "Отправитель",
					}
				}))
				.Cell(0, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Text = returnToSupplier.AddressName,
					}
				})
				.Cell(1, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Text = "Получатель"
					}
				})
				.Cell(1, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Text = returnToSupplier.SupplierName,
					}
				})
				.Cell(2, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Text = "Комментарий",
					}
				})
				.Cell(2, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Text = "",
					}
				});
			doc.Blocks.Add(new BlockUIContainer(header));
			doc.Blocks.Add(new BlockUIContainer(body));
		}
	}
}
