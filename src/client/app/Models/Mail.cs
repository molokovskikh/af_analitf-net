using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools.Calendar;

namespace AnalitF.Net.Client.Models
{
	public class Mail : BaseNotify
	{
		private bool isNew;
		private bool isImportant;

		public Mail(string subject)
			: this()
		{
			Subject = subject;
			SentAt = DateTime.Now;
		}

		public Mail()
		{
			Attachments = new List<Attachment>();
			IsNew = true;
		}

		public virtual uint Id { get; set; }

		public virtual bool IsNew
		{
			get { return isNew; }
			set
			{
				if (isNew == value)
					return;
				isNew = value;
				IsEdited = true;
				OnPropertyChanged();
			}
		}

		public virtual bool IsImportant
		{
			get { return isImportant; }
			set
			{
				isImportant = value;
				OnPropertyChanged();
			}
		}

		public virtual bool IsSpecial { get; set; }
		public virtual DateTime SentAt { get; set; }
		public virtual string Sender { get; set; }
		public virtual string SenderEmail { get; set; }
		public virtual string Subject { get; set; }
		public virtual string Body { get; set; }
		public virtual IList<Attachment> Attachments { get; set; }

		[Ignore]
		public virtual bool IsEdited { get; set; }

		public virtual string SenderUri
		{
			get { return "mailto:" + SenderEmail; }
		}

		public virtual bool HaveAttachments
		{
			get { return Attachments.Count > 0; }
		}

		public override string ToString()
		{
			return String.Format("{0} - {1}", SentAt, Subject);
		}

		public static void TrackIsNew(IScheduler scheduler, NotifyValue<Mail> current)
		{
			current
				.Do(_ => {
					if (current.Value != null)
						current.Value.IsEdited = false;
				})
				.Throttle(3.Second(), scheduler)
				.Select(_ => current.Value)
				.Where(v => v != null && v.IsNew && !v.IsEdited)
				.Subscribe(v => {
					v.IsNew = false;
				});
		}
	}
}