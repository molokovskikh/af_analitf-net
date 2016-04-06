using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnalitF.Net.Client.Views.Dialogs;
using Common.Tools;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AnalitF.Net.Client.Models
{
	public class PriceTag : INotifyPropertyChanged
	{
		private double _width;
		private double _height;

		public PriceTag()
		{
			Items = new List<PriceTagItem>();
		}

		public virtual uint Id { get; set; }

		public virtual double Height
		{
			get { return _height; }
			set
			{
				if (_height == value)
					return;
				_height = value;
				OnPropertyChanged();
			}
		}

		public virtual double Width
		{
			get { return _width; }
			set
			{
				if (_width == value)
					return;
				_width = value;
				OnPropertyChanged();
			}
		}

		[Write(false)]
		public virtual IList<PriceTagItem> Items { get; set; }

		public static Border Preview(double width, double height, IList<PriceTagItem> items)
		{
			return new Border {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0.5),
				Child = PriceTagItem.ToElement(width, height, items, PriceTagItem.DemoData),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
		}

		public virtual Border Preview()
		{
			return Preview(Height, Width, Items);
		}

		public static PriceTag LoadOrDefault(IDbConnection connection)
		{
				var tag = connection.Query<PriceTag>("select * from PriceTags").FirstOrDefault();
			if (tag != null)
				tag.Items = connection.Query<PriceTagItem>("select * from PriceTagItems order by Position").ToArray();
			else
				tag = Default();
			return tag;
		}

		public static PriceTag Default()
		{
			return new PriceTag {
				Height = 5,
				Width = 5,
				Items = {
					new PriceTagItem("Наименование клиента") {
						BorderThickness = 1,
						BottomBorder = true,
						TextAlignment = TextAlignment.Center,
						LeftMargin = 2,
						RightMargin = 2
					},
					new PriceTagItem("Наименование") {
						Underline = true,
						Bold = true,
						TextAlignment = TextAlignment.Center,
						Wrap = true,
						LeftMargin = 2,
						RightMargin = 2,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Цена",
						LeftMargin = 2,
					},
					new PriceTagItem("Цена") {
						IsNewLine = false,
						Bold = true,
						FontSize = 20,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Произв.",
						LeftMargin = 2,
					},
					new PriceTagItem("Страна") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2
					},
					new PriceTagItem("Производитель") {
						LeftMargin = 2,
						RightMargin = 2,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Срок годности",
						LeftMargin = 2,
					},
					new PriceTagItem("Срок годности") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Серия товара",
						LeftMargin = 2,
						FontSize = 10
					},
					new PriceTagItem("Серия товара") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						FontSize = 10
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "№ накладной",
						LeftMargin = 2,
						FontSize = 10
					},
					new PriceTagItem("Номер накладной") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						FontSize = 10
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Подпись",
						LeftMargin = 2,
						BorderThickness = 1,
						TopBorder = true,
						FontSize = 10
					},
					new PriceTagItem("Дата накладной") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						FontSize = 10,
						BorderThickness = 1,
						TopBorder = true
					},
				}
			};
		}

		public virtual FrameworkElement ToElement(WaybillLine line)
		{
			return PriceTagItem.ToElement(Width, Height, Items, PriceTagItem.Map(line));
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class PriceTagItem : INotifyPropertyChanged
	{
		public static Dictionary<string, string> DemoData = new Dictionary<string, string> {
			{"Цена", "221,03"},
			{"Наименование клиента", "Здоровая Аптека"},
			{"Наименование", "Доксазозин 4мг таб. Х30 (R)"},
			{"Страна", "РОССИЯ"},
			{"Производитель", "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ"},
			{"Срок годности", "01.05.2017"},
			{"Номер накладной", "6578"},
			{"Поставщик", "Катрен"},
			{"Серия товара", "21012"},
			{"Дата накладной", DateTime.Today.ToShortDateString() },
		};

		private bool _isNewLine;

		public PriceTagItem()
		{
		}

		public PriceTagItem(PriceTagItem item)
			: this(item.Name)
		{
		}

		public PriceTagItem(string name)
		{
			Name = name;
			IsNewLine = true;
			FontSize = 14;
			TextAlignment = TextAlignment.Left;
		}

		public virtual uint Id { get; set; }
		public virtual int Position { get; set; }
		public virtual string Name { get; set; }
		public virtual string Text { get; set; }
		[Write(false)]
		public virtual bool IsTextVisible => Name == "<Произвольный текст>";
		[Write(false)]
		public virtual string DisplayText => Text == null ? Name : $"{Name} - {Text}";

		public virtual bool IsNewLine
		{
			get { return _isNewLine; }
			set
			{
				if (_isNewLine == value)
					return;
				_isNewLine = value;
				OnPropertyChanged();
			}
		}

		public virtual TextAlignment TextAlignment { get; set; }
		public virtual double FontSize { get; set; }
		public virtual bool Wrap { get; set; }
		public virtual bool Bold { get; set; }
		public virtual bool Italic { get; set; }
		public virtual bool Underline { get; set; }
		public virtual double LeftMargin { get; set; }
		public virtual double TopMargin { get; set; }
		public virtual double BottomMargin { get; set; }
		public virtual double RightMargin { get; set; }
		public virtual double BorderThickness { get; set; }
		public virtual bool LeftBorder { get; set; }
		public virtual bool TopBorder { get; set; }
		public virtual bool RightBorder { get; set; }
		public virtual bool BottomBorder { get; set; }

		public static PriceTagItem[] Items()
		{
			return new [] {
				new PriceTagItem("<Произвольный текст>"),
				new PriceTagItem("Цена"),
				new PriceTagItem("Наименование клиента"),
				new PriceTagItem("Наименование"),
				new PriceTagItem("Страна"),
				new PriceTagItem("Производитель"),
				new PriceTagItem("Срок годности"),
				new PriceTagItem("Номер накладной"),
				new PriceTagItem("Поставщик"),
				new PriceTagItem("Серия товара"),
				new PriceTagItem("Дата накладной")
			};
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public static FrameworkElement ToElement(double width, double height,
			IEnumerable<PriceTagItem> items,
			Dictionary<string, string> dataMap)
		{
			var panel = new StaticCanvasPanel();
			foreach (var src in items) {
				var dstText = new TextBlock {
					Text = dataMap.GetValueOrDefault(src.Name, src.Text),
					FontSize = src.FontSize,
					TextAlignment = src.TextAlignment,
					TextWrapping = src.Wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
					FontWeight = src.Bold ? FontWeights.Bold : FontWeights.Normal,
					FontStyle = src.Italic ? FontStyles.Italic : FontStyles.Normal,
					TextDecorations = src.Underline ? TextDecorations.Underline : null,
					Margin = new Thickness(src.LeftMargin, src.TopMargin, src.RightMargin, src.BottomMargin),
				};
				var dst = new Border {
					BorderThickness = new Thickness(src.LeftBorder ? src.BorderThickness : 0,
						src.TopBorder ? src.BorderThickness : 0,
						src.RightBorder ? src.BorderThickness : 0,
						src.BottomBorder ? src.BorderThickness : 0),
					BorderBrush = Brushes.Black,
					Child = dstText,
				};
				CanvasPanel.SetIsNewLine(dst, src.IsNewLine);
				panel.Children.Add(dst);
			}
			panel.Width = CmToPx(width);
			panel.Height = CmToPx(height);
			return panel;
		}

		public static double CmToPx(double value)
		{
			return value / 2.54 * 96d;
		}

		public static double MmToPx(double value)
		{
			return value / 10 / 2.54 * 96d;
		}

		public static double PxToMm(double value)
		{
			return value * 10 * 2.54 / 96d;
		}

		public static Dictionary<string, string> Map(WaybillLine line)
		{
			return new Dictionary<string, string> {
				{"Цена", line.RetailCost.ToString()},
				{"Наименование клиента", line.Waybill.WaybillSettings.FullName},
				{"Наименование", line.Product},
				{"Страна", line.Country},
				{"Производитель", line.Producer},
				{"Срок годности", line.Period},
				{"Номер накладной", line.Waybill.ProviderDocumentId},
				{"Поставщик", line.Waybill.SupplierName},
				{"Серия товара", line.SerialNumber},
				{"Дата накладной", line.Waybill.DocumentDate.ToShortDateString() },
			};
		}
	}
}