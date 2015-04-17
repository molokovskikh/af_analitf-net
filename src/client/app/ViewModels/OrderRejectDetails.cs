using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Offers;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class OrderRejectDetails : BaseScreen
	{
		private uint id;

		public OrderRejectDetails(uint id)
		{
			this.id = id;
			Doc = new NotifyValue<OrderReject>();
			Lines = new NotifyValue<List<OrderRejectLine>>();
			CurrentLine = new NotifyValue<OrderRejectLine>();
		}

		public NotifyValue<OrderRejectLine> CurrentLine { get; set; }
		public NotifyValue<List<OrderRejectLine>> Lines { get; set; }
		public NotifyValue<OrderReject> Doc { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Doc.Value = Session.Query<OrderReject>().First(r => r.DownloadId == id);
			Lines.Value = Doc.Value.Lines.OrderBy(l => l.Product).ToList();
		}

		public void EnterLine()
		{
			if (CurrentLine.Value == null)
				return;
			Shell.Navigate(new SearchOfferViewModel(CurrentLine.Value.Product));
		}
	}
}