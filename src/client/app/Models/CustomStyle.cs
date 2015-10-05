using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Color = System.Drawing.Color;
using Control = System.Windows.Controls.Control;

namespace AnalitF.Net.Client.Models
{
	public class CustomStyle : BaseNotify, IEquatable<CustomStyle>
	{
		private string foreground;
		private string background;

		public CustomStyle()
		{
			Background = "White";
			Foreground = "Black";
		}

		public CustomStyle(string name, string background)
			: this()
		{
			Name = name;
			Description = name;
			Background = background;
			IsBackground = true;
		}

		public virtual uint Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }

		public virtual string Background
		{
			get { return background; }
			set
			{
				if (background == value)
					return;
				background = value;
				OnPropertyChanged();
			}
		}

		public virtual string Foreground
		{
			get { return foreground; }
			set
			{
				if (foreground == value)
					return;
				foreground = value;
				OnPropertyChanged();
			}
		}

		public virtual bool IsBackground { get; set; }

		[Ignore]
		public virtual System.Windows.Media.Color Color
		{
			get
			{
				if (IsBackground)
					return (System.Windows.Media.Color)ColorConverter.ConvertFromString(Background);
				return (System.Windows.Media.Color)ColorConverter.ConvertFromString(Foreground);
			}
			set
			{
				if (IsBackground)
					Background = value.ToString();
				else
					Foreground = value.ToString();
			}
		}

		public virtual bool Equals(CustomStyle other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return String.Equals(Name, other.Name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((CustomStyle)obj);
		}

		public override int GetHashCode()
		{
			return Name?.GetHashCode() ?? 0;
		}

		public virtual Setter ToSetter()
		{
			if (IsBackground) {
				return new Setter(Control.BackgroundProperty, new SolidColorBrush(Color));
			}
			else {
				return new Setter(Control.ForegroundProperty, new SolidColorBrush(Color));
			}
		}

		public static IEnumerable<IResult> Edit(CustomStyle style)
		{
			if (style == null)
				yield break;
			var converter = TypeDescriptor.GetConverter(typeof(Color));
			var dialog = new ColorDialog {
				Color = style.IsBackground
					? (Color)converter.ConvertFrom(style.Background)
					: (Color)converter.ConvertFrom(style.Foreground),
				FullOpen = true,
			};
			yield return new NativeDialogResult<ColorDialog>(dialog);
			var value = dialog.Color.ToHexString();
			if (style.IsBackground) {
				style.Background = value;
			}
			else {
				style.Foreground = value;
			}
		}
	}

	public static class ColorHelper
	{
		public static string ToHexString(this Color color)
		{
			var value = String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
			return value;
		}
	}
}