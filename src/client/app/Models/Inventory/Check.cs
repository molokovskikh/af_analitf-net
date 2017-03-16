using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum CheckType
	{
		[Description("Продажа покупателю")] SaleBuyer,
		[Description("Возврат по чеку")] CheckReturn,
	}

	public enum PaymentType
	{
		[Description("Наличный рубль")] Cash,
		[Description("Безналичный рубль")] Cashless
	}

	public enum Status
	{
		[Description("Закрыт")] Closed,
		[Description("Открыт")] Open,
	}

	public enum SaleType
	{
		[Description("Полная стоимость")] FullCost,
	}

	public class Check : BaseNotify, IStockDocument
	{
		private bool _new;
		private uint _id;
		private string _numberprefix;
		private string _numberdoc;

		public Check(User user, Address address, string numberprefix, IEnumerable<CheckLine> lines, CheckType checkType)
			: this()
		{
			_numberprefix = numberprefix;
			_new = true;
			Timestamp = DateTime.Now;
			Clerk = user.Id.ToString();
			CheckType = checkType;
			Date = DateTime.Now;
			ChangeOpening = DateTime.Today;
			Status = Status.Closed;
			Address = address;
			PaymentType = PaymentType.Cash;
			SaleType = SaleType.FullCost;
			Lines.AddEach(lines);
			UpdateStat();

		}

		public Check()
		{
			Lines = new List<CheckLine>();
		}

		public virtual uint Id
		{
			get { return _id; }
			set
			{
				_id = value;
				if (_new)
					NumberDoc = _numberprefix + Id.ToString("d8");
			}
		}
		public virtual string DisplayName { get { return "Чек"; } }
		public virtual string NumberDoc
		{
			get { return !String.IsNullOrEmpty(_numberdoc) ? _numberdoc : Id.ToString("d8"); }
			set { _numberdoc = value; }
		}
		public virtual string FromIn
		{ get { return string.Empty; } }
		public virtual string OutTo
		{ get { return "Покупатель"; } }

		public virtual uint? ServerId { get; set; }
		public virtual CheckType CheckType { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual DateTime ChangeOpening { get; set; }
		public virtual Status Status { get; set; }

		public virtual string Clerk { get; set; } //Пока пусть будет строка
		public virtual Address Address { get; set; }

		public virtual string KKM { get; set; }
		public virtual PaymentType PaymentType { get; set; }
		public virtual SaleType SaleType { get; set; }
		public virtual uint Discont { get; set; }
		public virtual uint ChangeId { get; set; }
		public virtual uint ChangeNumber { get; set; }
		[Style(Description = "\"Аннулирован\"")]
		public virtual bool Cancelled { get; set; }

		/// <summary>
		/// Получено, руб
		/// </summary>
		public virtual decimal Payment { get; set; }
		/// <summary>
		/// Сдача, руб
		/// </summary>
		public virtual decimal Charge { get; set; }
		public virtual decimal Sum => RetailSum - DiscountSum;
		public virtual decimal RetailSum { get; set; }
		public virtual decimal DiscountSum { get; set; }
		public virtual decimal SupplySum { get; set; }

		public virtual DateTime Timestamp { get; set; }

		public virtual IList<CheckLine> Lines { get; set; }

		public virtual void UpdateStat()
		{
			RetailSum = Lines.Sum(l => l.RetailSum);
			DiscountSum = Lines.Sum(l => l.DiscontSum);
			SupplySum = Lines.Sum(x => x.Quantity * x.SupplierCost.GetValueOrDefault());
		}

		public virtual void Print(string printer, WaybillSettings settings)
		{
			var dialog = new PrintDialog();
			dialog.PrintQueue = new PrintQueue(new PrintServer(), printer);

			var doc = new FlowDocument();
			doc.ColumnGap = 0;
			doc.PagePadding = new Thickness(0, 0, 0, 0);
			doc.ColumnWidth = double.PositiveInfinity;
			doc.FontFamily = new FontFamily("Arial");
			var width = dialog.PrintableAreaWidth;
			doc.PageWidth = width;
			doc.Blocks.Add(new Paragraph(new Run(settings.FullName) {
				FontSize = 20,
			}) {
				TextAlignment = TextAlignment.Center
			});
			var table = new Table();
			table.CellSpacing = 0;
			var value = width / 2;
			table.Columns.Add(new TableColumn {
				Width = new GridLength(value, GridUnitType.Pixel)
			});
			table.Columns.Add(new TableColumn {
				Width = new GridLength(value, GridUnitType.Pixel)
			});
			var tableRowGroup = new TableRowGroup();
			tableRowGroup.Rows.Add(new TableRow {
				Cells = {
					new TableCell(new Paragraph(new Run($"Чек №{Id}"))) {
						FontSize = 8,
					},
					new TableCell(new Paragraph(new Run(Date.ToString()))) {
						TextAlignment = TextAlignment.Right,
						FontSize = 8
					},
				}
			});
			table.RowGroups.Add(tableRowGroup);
			var paragraph = new Paragraph() {
				FontSize = 10,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 1, 0, 0)
			};
			doc.Blocks.Add(table);
			doc.Blocks.Add(paragraph);
			foreach (var line in Lines) {
				paragraph.Inlines.Add(new Run($"{line.Product} {line.RetailCost:C} x {line.Quantity} = {line.Sum:C}"));
				paragraph.Inlines.Add(new LineBreak());
			}
			doc.Blocks.Add(new Paragraph() {
				Inlines = {
					new Run($"Итого = {Sum:C}") {
						FontSize = 20,
						FontWeight = FontWeights.Bold,
					},
					new LineBreak(),
					new Run($"Наличными = {Payment:C}") {
						FontSize = 10,
					},
					new LineBreak(),
					new Run($"Сдача = {Charge:C}") {
						FontSize = 10
					}
				},
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 1, 0, 0)
			});

			doc.Blocks.Add(new Paragraph(new LineBreak()));
			doc.Blocks.Add(new Paragraph(new LineBreak()));

			dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Чек");
		}
	}
}
