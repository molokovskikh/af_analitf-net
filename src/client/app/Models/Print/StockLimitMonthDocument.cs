using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class StockLimitMonthDocument : BaseDocument
	{
		private Stock[] _stocks;
		private string _title;
		private string _name;

		public StockLimitMonthDocument(Stock[] stocks, string title, string name)
		{
			_stocks = stocks;
			_title = title;
			_name = name;
		}

		protected override void BuildDoc()
		{
			Landscape();

			Block(_name);
			Block("");

			var block = new Paragraph { Style = BlockStyle };
			//Figure - игнорирует отступы
			block.Inlines.Add(new Figure(new Paragraph(new Run(_title)))
			{
				HorizontalAnchor = FigureHorizontalAnchor.ContentCenter,
				TextAlignment = TextAlignment.Center
			});
			doc.Blocks.Add(block);

			Block("");
			Block($"Дата отчёта " + DateTime.Today.ToShortDateString());

			var headers = new[] {
				new PrintColumn("№", 30),
				new PrintColumn("Док. ИД", 60),
				new PrintColumn("Название товара", 200),
				new PrintColumn("Серия", 60),
				new PrintColumn("Производитель", 180),
				new PrintColumn("Партия №", 80),
				new PrintColumn("Кол-во", 60),
				new PrintColumn("Срок годности", 100),
				new PrintColumn("Дата документа", 100),
				new PrintColumn("Цена закупки", 80)
			};

			var rows = _stocks.Select((o, i) => new object[] {
				i + 1,
				o.ProductId,
				o.Product,
				o.SerialNumber,
				o.Producer,
				"",
				o.Quantity,
				o.Period,
				o.DocumentDate,
				o.RetailCost
			});

			var table = BuildTable(rows, headers);
		}
	}
}
