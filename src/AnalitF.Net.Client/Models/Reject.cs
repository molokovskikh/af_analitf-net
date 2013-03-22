using System;
using System.ComponentModel;

namespace AnalitF.Net.Client.Models
{
	public class Reject : INotifyPropertyChanged
	{
		private bool marked;

		public virtual uint Id { get; set; }

		public virtual string Product { get; set; }

		public virtual uint? ProductId { get; set; }

		public virtual string Producer { get; set; }

		public virtual uint? ProducerId { get; set; }

		public virtual string Series { get; set; }

		public virtual string LetterNo { get; set; }

		public virtual DateTime LetterDate { get; set; }

		public virtual string CauseRejects { get; set; }

		public virtual bool Marked
		{
			get { return marked; }
			set
			{
				if (marked == value)
					return;

				marked = value;
				OnPropertyChanged("Marked");
			}
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}