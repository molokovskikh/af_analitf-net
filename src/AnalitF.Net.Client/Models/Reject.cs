using System;
using System.ComponentModel;

namespace AnalitF.Net.Client.Models
{
	public class Reject : BaseStatelessObject
	{
		private bool marked;

		public override uint Id { get; set; }

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
	}
}