using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace AnalitF.Net.Client.Models
{
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

		public virtual uint Id { get; set; }
		public virtual int Position { get; set; }
		public virtual string Name { get; set; }
		public virtual string Text { get; set; }
		public virtual bool IsTextVisible => Name == "<Произвольный текст>";
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
	}
}