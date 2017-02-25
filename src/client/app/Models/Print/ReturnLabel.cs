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
		private ReturnDoc returnToSupplier;
		private WaybillSettings waybillSettings;
		public ReturnLabel(ReturnDoc _returnToSupplier, WaybillSettings _waybillSettings)
		{
			returnToSupplier = _returnToSupplier;
			waybillSettings = _waybillSettings;

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

			var body = new Grid()
				.Cell(0, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 24,
						Text = "ВОЗВРАТ",
						TextAlignment = TextAlignment.Center
					}
				})
				.Cell(1, 0, new Grid()
				.Cell(0, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 20,
						Text = "Отправитель",
					}
				})
				.Cell(0, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 16,
						Text = returnToSupplier.SupplierName + "\n" + returnToSupplier.AddressName,
					}
				})
				.Cell(1, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 20,
						Text = "Получатель"
					}
				})
				.Cell(1, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 16,
						Text = waybillSettings==null ? "" : waybillSettings.FullName
					}
				})
				.Cell(2, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 20,
						Text = "Комментарий",
					}
				})
				.Cell(2, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 16,
						Text = returnToSupplier.Comment,
					}
				}));
			var border = new Border
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1, 1, 1, 1),
				Child = body,
			};
			doc.Blocks.Add(new BlockUIContainer(border));
		}
	}
}
