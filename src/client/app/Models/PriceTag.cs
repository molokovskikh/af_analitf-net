using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Views.Dialogs;
using Common.Tools;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AnalitF.Net.Client.Models
{
	public enum TagType
	{
		PriceTag,
		RackingMap
	}

	public class PriceTag : INotifyPropertyChanged
	{
		private double _width;
		private double _height;
		private double _borderThickness;

		public PriceTag()
		{
			Items = new List<PriceTagItem>();
		}

		public PriceTag(PriceTag source, Address address) : this()
		{
			AddressId = address.Id;
			BorderThickness = source.BorderThickness;
			Height = source.Height;
			TagType = source.TagType;
			Width = source.Width;
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

		public virtual TagType TagType { get; set; }

		public virtual uint? AddressId { get; set; }

		public virtual double BorderThickness
		{
			get { return _borderThickness; }
			set
			{
				if (_borderThickness == value)
					return;
				_borderThickness = value;
				OnPropertyChanged();
			}
		}

		[Write(false)]
		public virtual IList<PriceTagItem> Items { get; set; }

		public static Border Preview(double width, double height, double borderThickness, IList<PriceTagItem> items)
		{
			return new Border {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(borderThickness),
				Child = PriceTagItem.ToElement(width, height, items, DemoData),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
		}

		public virtual Border Preview()
		{
			return Preview(Width, Height, BorderThickness, Items);
		}

		public static PriceTag LoadOrDefault(IDbConnection connection, TagType tagType, Address address)
		{
			var sql = tagType == TagType.RackingMap
				? $"select * from PriceTags where TagType = {(int) tagType}"
				: $"select * from PriceTags where TagType = {(int) tagType} and AddressId = {address.Id}";
			var tag = connection.Query<PriceTag>(sql).FirstOrDefault();
			if (tag != null) {
				tag.Items = connection.Query<PriceTagItem>($"select * from PriceTagItems where PriceTagId = {tag.Id} order by Position").ToArray();
			}
			else
				tag = Default(tagType, address);
			return tag;
		}

		public static PriceTag Default(TagType tagType, Address address)
		{
			var tag = new PriceTag {
				TagType = tagType,
				AddressId = tagType == TagType.PriceTag ? address.Id : (uint?)null,
				Height = 5,
				Width = 5,
				BorderThickness = 0.5d,
				Items = {
					new PriceTagItem("Наименование клиента") {
						BorderThickness = 1,
						BottomBorder = true,
						TextAlignment = TextAlignment.Center,
						LeftMargin = 2,
						RightMargin = 2,
						Position = 1,
					},
					new PriceTagItem("Наименование") {
						Underline = true,
						Bold = true,
						TextAlignment = TextAlignment.Center,
						Wrap = true,
						LeftMargin = 2,
						RightMargin = 2,
						Position = 2,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Цена",
						LeftMargin = 2,
						Position = 3,
					},
					new PriceTagItem("Цена") {
						IsNewLine = false,
						Bold = true,
						FontSize = 20,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						Position = 4,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Произв.",
						LeftMargin = 2,
						Position = 5,
					},
					new PriceTagItem("Страна") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						Position = 6,
					},
					new PriceTagItem("Производитель") {
						LeftMargin = 2,
						RightMargin = 2,
						Position = 7,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Срок годности",
						LeftMargin = 2,
						Position = 8,
					},
					new PriceTagItem("Срок годности") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						Position = 9,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Серия товара",
						LeftMargin = 2,
						FontSize = 10,
						Position = 10,
					},
					new PriceTagItem("Серия товара") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						FontSize = 10,
						Position = 11,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "№ накладной",
						LeftMargin = 2,
						FontSize = 10,
						Position = 16,
					},
					new PriceTagItem("Номер накладной") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						FontSize = 10,
						Position = 17,
					},
					new PriceTagItem("<Произвольный текст>") {
						Text = "Подпись",
						LeftMargin = 2,
						BorderThickness = 1,
						TopBorder = true,
						FontSize = 10,
						Position = 18,
					},
					new PriceTagItem("Дата накладной") {
						IsNewLine = false,
						TextAlignment = TextAlignment.Right,
						RightMargin = 2,
						FontSize = 10,
						BorderThickness = 1,
						TopBorder = true,
						Position = 19,
					},
				}
			};
			if (tagType == TagType.RackingMap) {
				tag.Height = 6;
				tag.Items.Add(new PriceTagItem("<Произвольный текст>") {
					Text = "Количество",
					LeftMargin = 2,
					FontSize = 10,
					Position = 12,
				});
				tag.Items.Add(new PriceTagItem("Количество") {
					IsNewLine = false,
					TextAlignment = TextAlignment.Right,
					RightMargin = 2,
					FontSize = 10,
					Position = 13,
				});
				tag.Items.Add(new PriceTagItem("<Произвольный текст>") {
					Text = "Номер сертификата",
					LeftMargin = 2,
					FontSize = 10,
					Position = 14,
				});
				tag.Items.Add(new PriceTagItem("Номер сертификата")
				{
					IsNewLine = false,
					TextAlignment = TextAlignment.Right,
					RightMargin = 2,
					FontSize = 10,
					Position = 15,
				});
				tag.Items = tag.Items.OrderBy(x => x.Position).ToList();
			}
			return tag;
		}

		public virtual FrameworkElement ToElement(TagPrintable line, Address address = null)
		{
			return PriceTagItem.ToElement(Width, Height, Items, PriceTagItem.Map(line, address));
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public static Dictionary<string, string> DemoData = new Dictionary<string, string> {
			{"Цена", "221-03"},
			{"Наименование клиента", "Здоровая Аптека, ул.Крылова, 7а"},
			{"Наименование", "Доксазозин 4мг таб. Х30 (R)"},
			{"Страна", "РОССИЯ"},
			{"Производитель", "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ"},
			{"Срок годности", "01.05.2017"},
			{"Номер накладной", "6578"},
			{"Поставщик", "Катрен"},
			{"Серия товара", "21012"},
			{"Дата накладной", DateTime.Today.ToShortDateString() },
			{"Количество", "100" },
			{"Номер сертификата", "RU.АЯ46.В60125" },
		};
	}

	public class PriceTagItem : INotifyPropertyChanged
	{
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

		public PriceTagItem(PriceTagItem source, PriceTag priceTag)
		{
			Bold = source.Bold;
			BorderThickness = source.BorderThickness;
			BottomBorder = source.BottomBorder;
			BottomMargin = source.BottomMargin;
			FontSize = source.FontSize;
			Height = source.Height;
			IsAutoHeight = source.IsAutoHeight;
			IsAutoWidth = source.IsAutoWidth;
			IsNewLine = source.IsNewLine;
			Italic = source.Italic;
			LeftBorder = source.LeftBorder;
			LeftMargin = source.LeftMargin;
			Name = source.Name;
			Position = source.Position;
			PriceTagId = priceTag.Id;
			RightBorder = source.RightBorder;
			RightMargin = source.RightMargin;
			Text = source.Text;
			TextAlignment = source.TextAlignment;
			TopBorder = source.TopBorder;
			TopMargin = source.TopMargin;
			Underline = source.Underline;
			Width = source.Width;
			Wrap = source.Wrap;
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
		public virtual uint PriceTagId { get; set; }
		public virtual bool IsAutoWidth { get; set; }
		public virtual double? Width { get; set; }
		public virtual bool IsAutoHeight { get; set; }
		public virtual double? Height { get; set; }

		public static PriceTagItem[] Items(TagType tagType)
		{
			var items = new List<PriceTagItem> {
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
			if (tagType == TagType.RackingMap) {
				items.Add(new PriceTagItem("Количество"));
				items.Add(new PriceTagItem("Номер сертификата"));
			}
			return items.ToArray();
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
					Height = src.IsAutoHeight ? double.NaN : CmToPx(src.Height ?? double.NaN),
					Width = src.IsAutoWidth ? double.NaN : CmToPx(src.Width ?? double.NaN),
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

		private static NumberFormatInfo GetFormat()
		{
			var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
			format.NumberDecimalSeparator = "-";
			return format;
		}

		public static Dictionary<string, string> Map(TagPrintable line, Address address)
		{
			return new Dictionary<string, string> {
				{"Цена", String.Format(GetFormat(), "{0:0.00}", line.RetailCost)},
				{"Наименование клиента", address == null ? line.ClientName : $"{line.ClientName}, {address.Name}"},
				{"Наименование", line.Product},
				{"Страна", line.Country},
				{"Производитель", line.Producer},
				{"Срок годности", line.Period},
				{"Номер накладной", line.ProviderDocumentId},
				{"Поставщик", line.SupplierName},
				{"Серия товара", line.SerialNumber},
				{"Дата накладной", line.DocumentDate.ToShortDateString() },
				{"Количество", line.Quantity.ToString() },
				{"Номер сертификата", line.Certificates },
			};
		}
	}
}