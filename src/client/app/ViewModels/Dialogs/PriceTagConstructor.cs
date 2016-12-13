using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;
using NHibernate;
using System.Windows.Threading;
using Common.NHibernate;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PriceTagConstructor : BaseScreen
	{
		private TagType tagType;
		private Address address;

		public PriceTagConstructor(TagType tagType, Address address)
		{
			this.tagType = tagType;
			this.address = address;
			InitFields();
			DisplayName = tagType == TagType.PriceTag ? "Конструктор ценника" : "Конструктор стеллажной карты";
			Alignments = new[] {
				new KeyValuePair<string, TextAlignment>("По левому краю", TextAlignment.Left),
				new KeyValuePair<string, TextAlignment>("По центру", TextAlignment.Center),
				new KeyValuePair<string, TextAlignment>("По правому краю", TextAlignment.Right),
			};
			Fields = PriceTagItem.Items(tagType);

			Items = new ObservableCollection<PriceTagItem>();
			Items.CollectionChanged += (sender, args) => {
				Preview();
			};
			Selected.Subscribe(x => {
				SelectedItem.Value = (PriceTagItem)x?.DataContext;
				foreach (var property in typeof(PriceTagItem).GetProperties())
					OnPropertyChanged(property.Name);
			});
			Tag.ChangedValue().Subscribe(x => Preview());
			Tag.Subscribe(x => {
				Items.Clear();
				Items.AddEach(Tag.Value?.Items ?? Enumerable.Empty<PriceTagItem>());
			});

			Tag.Value =
				Session.Query<PriceTag>()
					.FirstOrDefault(r => r.TagType == tagType && (tagType == TagType.RackingMap || r.Address == address));
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, tagType, address)).Subscribe(Tag);
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

		public double? Width
		{
			get
			{
				return SelectedItem.Value?.Width;
			}
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Width = value;
					SelectedItem.Value.IsAutoWidth = value == null;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public double? Height
		{
			get
			{
				return SelectedItem.Value?.Height;
			}
			set
			{
				if (SelectedItem.Value != null) {
					SelectedItem.Value.Height = value;
					SelectedItem.Value.IsAutoHeight = value == null;
					Preview();
					OnPropertyChanged();
				}
			}
		}

		public string Name => SelectedItem.Value?.Name;

		public NotifyValue<PriceTag> Tag { get; set; }
		public NotifyValue<Border> PreviewContent { get; set; }
		public PriceTagItem[] Fields { get; set; }
		public ObservableCollection<PriceTagItem> Items { get; set; }
		public NotifyValue<PriceTagItem> SelectedItem { get; set; }
		public NotifyValue<FrameworkElement> Selected { get; set; }

		public void Preview()
		{
			if (Tag.Value == null)
				return;
			PreviewContent.Value = PriceTag.Preview(Tag.Value.Width, Tag.Value.Height, Tag.Value.BorderThickness, Items);
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		public void Save()
		{
			var session = Session.SessionFactory.OpenStatelessSession();
			var tag = Tag.Value;
			var tx = session.BeginTransaction();

			try
			{
				if (tag.Id == 0)
					session.Insert(tag);
				else
					session.Update(tag);

				foreach (var item in tag.Items)
				{
					item.PriceTag = tag;
					if (item.Id == 0)
						session.Insert(item);
					else
						session.Update(item);
				}

				var items = session.Query<PriceTagItem>().Where(r => r.PriceTag == tag);
				session.DeleteEach(items.Where(r => !tag.Items.Contains(r)));

				tx.Commit();
			}
			catch
			{
				tx.Rollback();
				throw;
			}
			finally
			{
				session.Close();
			}

			TryClose();
		}

		public void Clear()
		{
			Items.Clear();
		}

		public void Reset()
		{
			Tag.Value = PriceTag.Default(tagType, address);
		}

		public void Delete()
		{
			if (SelectedItem.Value != null)
				Items.Remove(SelectedItem.Value);
		}
	}
}