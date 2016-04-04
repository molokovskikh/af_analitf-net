using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using Dapper;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PriceTagConstructor : BaseScreen
	{
			private Dictionary<string, string> dataMap = new Dictionary<string, string> {
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

		public PriceTagConstructor()
		{
			InitFields(this);
			Alignments = new[] {
				new KeyValuePair<string, TextAlignment>("По левому краю", TextAlignment.Left),
				new KeyValuePair<string, TextAlignment>("По центру", TextAlignment.Center),
				new KeyValuePair<string, TextAlignment>("По правому краю", TextAlignment.Right),
			};
			Width.Value = 5;
			Height.Value = 5;
			Fields = PriceTagItem.Items();

			Items = new ObservableCollection<PriceTagItem>();
			Items.CollectionChanged += (sender, args) => {
				Preview();
			};
			Selected.Subscribe(x => {
				SelectedItem.Value = (PriceTagItem)x?.DataContext;
				foreach (var property in typeof(PriceTagItem).GetProperties())
					OnPropertyChanged(property.Name);
			});
			Width.Skip(1).Merge(Height.Skip(1)).Subscribe(_ => Preview());
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(s => s.Query<PriceTagItem>().OrderBy(x => x.Position).ToArray())
				.ObserveOn(UiScheduler)
				.Subscribe(x => Items.AddEach(x));
		}

		public string Text
		{
			get { return SelectedItem.Value?.Text; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Text = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool IsTextVisible => SelectedItem.Value?.IsTextVisible ?? false;

		public double FontSize
		{
			get { return SelectedItem.Value?.FontSize ?? 0; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.FontSize = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public KeyValuePair<string, TextAlignment>[] Alignments { get; set; }
		public TextAlignment TextAlignment
		{
			get { return SelectedItem.Value?.TextAlignment ?? TextAlignment.Left; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.TextAlignment = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool IsNewLine
		{
			get { return SelectedItem.Value?.IsNewLine ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.IsNewLine = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool Wrap
		{
			get { return SelectedItem.Value?.Wrap ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Wrap = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool Bold
		{
			get { return SelectedItem.Value?.Bold ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Bold = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool Italic
		{
			get { return SelectedItem.Value?.Italic ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Italic = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool Underline
		{
			get { return SelectedItem.Value?.Underline ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Underline = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public double BorderThickness
		{
			get { return SelectedItem.Value?.BorderThickness ?? 0; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.BorderThickness = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool LeftBorder
		{
			get { return SelectedItem.Value?.LeftBorder ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.LeftBorder = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool RightBorder
		{
			get { return SelectedItem.Value?.RightBorder ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.RightBorder = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool TopBorder
		{
			get { return SelectedItem.Value?.TopBorder ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.TopBorder = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public bool BottomBorder
		{
			get { return SelectedItem.Value?.BottomBorder ?? false; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.BottomBorder = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public double LeftMargin
		{
			get { return SelectedItem.Value?.LeftMargin ?? 0; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.LeftMargin = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public double TopMargin
		{
			get { return SelectedItem.Value?.TopMargin ?? 0; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.TopMargin = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public double RightMargin
		{
			get { return SelectedItem.Value?.RightMargin ?? 0; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.RightMargin = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}
		public double BottomMargin
		{
			get { return SelectedItem.Value?.BottomMargin ?? 0; }
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.BottomMargin = value;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public string Name => SelectedItem.Value?.Name;

		public NotifyValue<double> Width { get; set; }
		public NotifyValue<double> Height { get; set; }
		public NotifyValue<Border> PreviewContent { get; set; }
		public PriceTagItem[] Fields { get; set; }
		public ObservableCollection<PriceTagItem> Items { get; set; }
		public NotifyValue<PriceTagItem> SelectedItem { get; set; }
		public NotifyValue<FrameworkElement> Selected { get; set; }

		public void Preview()
		{
			var panel = new StaticCanvasPanel();
			foreach (var src in Items) {
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
			PreviewContent.Value = new Border {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0.5),
				Child = panel,
				Margin = new Thickness(2),
				Width = CmToPx(Width.Value),
				Height = CmToPx(Height.Value),
			};
		}

		public double PxToMm(double value)
		{
			return value * 10 * 2.54 / 96d;
		}

		public double MmToPx(double value)
		{
			return value / 10 / 2.54 * 96d;
		}

		private double CmToPx(double value)
		{
			return value / 2.54 * 96d;
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		public void Save()
		{
			Query(s => {
				var dbItems = s.Query<PriceTagItem>().ToArray();
				for(var i = 0; i < Items.Count; i++) {
					var item = Items[i];
					item.Position = i;
					if (item.Id == 0) {
						s.Insert(item);
					}  else {
						try {
							s.Update(item);
						} catch(StaleStateException) {
						}
					}
				}
				foreach (var item in dbItems.Where(x => Items.All(y => x.Id != y.Id)))
					s.Delete(item);
			}).Wait();
		}

		public void Clear()
		{
			Items.Clear();
		}

		public void Place()
		{
			Items.Clear();
			foreach (var field in Fields)
				Items.Add(new PriceTagItem(field.Name));
		}

		public void Delete()
		{
			if (SelectedItem.Value != null)
				Items.Remove(SelectedItem.Value);
		}
	}
}