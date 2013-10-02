using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public class Correction : BaseOfferViewModel
	{
		public Correction()
		{
			Lines = new List<OrderLine>();
			CurrentLine = new NotifyValue<OrderLine>();
		}

		public List<OrderLine> Lines { get; set; }
		public NotifyValue<OrderLine> CurrentLine { get; set; }
		public ProductInfo ProductInfo { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			CanSend = new NotifyValue<bool>(
				() => Address.Orders.Any(o => o.Send)
					&& Address.Orders.Count(o => o.Send && o.SendResult == OrderResultStatus.Reject) == 0,
				Address.BindableOrders.Changed());
			DisplayName = string.Format("Журнал отправки заказов для адреса {0}", Address.Name);
			ProductInfo = new ProductInfo(StatelessSession, Manager, Shell, CurrentLine);
			var view = GetView();
			if (view != null)
				Attach(view, ProductInfo.Bindings);
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Lines = Session.Query<OrderLine>().Where(l => l.SendResult != LineResultStatus.OK
				|| l.Order.SendResult != OrderResultStatus.OK)
				.Distinct()
				.ToList();
		}

		protected override void Query()
		{
		}

		public IEnumerable<IResult> Save()
		{
			var save = new SaveFileResult(DisplayName);
			yield return save;
			save.Write(OrderLine.SendReport(Lines));
		}

		public NotifyValue<bool> CanSend { get; set; }

		public IEnumerable<IResult> Send()
		{
			TryClose();
			return Shell.SendOrders(true);
		}

		public IEnumerable<IResult> LoadUpdate()
		{
			TryClose();
			return Shell.Update();
		}

		public void Edit()
		{
			TryClose();
			Shell.ShowOrderLines();
		}
	}
}