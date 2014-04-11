using System;
using System.Drawing;
using AnalitF.Net.Client.Helpers;
using NPOI.SS.Formula.Functions;

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
				background = value;
				OnPropertyChanged();
			}
		}

		public virtual string Foreground
		{
			get { return foreground; }
			set
			{
				foreground = value;
				OnPropertyChanged();
			}
		}

		public virtual bool IsBackground { get; set; }

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
			return (Name != null ? Name.GetHashCode() : 0);
		}

		public static string ToHexString(Color color)
		{
			var value = String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
			return value;
		}
	}
}