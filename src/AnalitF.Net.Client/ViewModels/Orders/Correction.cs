using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
		private uint? addressId;

		public Correction()
		{
			Lines = new List<OrderLine>();
			CurrentLine = new NotifyValue<OrderLine>();
		}

		public Correction(uint? addressId = null)
			: this()
		{
			this.addressId = addressId;
		}

		public List<OrderLine> Lines { get; set; }
		public NotifyValue<OrderLine> CurrentLine { get; set; }
		public ProductInfo ProductInfo { get; set; }
		public NotifyValue<bool> CanSend { get; set; }

		public bool IsOrderSend
		{
			get { return addressId != null; }
		}

		public bool IsUpdate
		{
			get { return addressId == null; }
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (IsUpdate)
				CurrentLine.Changed().Subscribe(_ => Update());

			CanSend = new NotifyValue<bool>(
				() => Address.Orders.Any(o => o.Send)
					&& Address.Orders.Count(o => o.Send && o.SendResult == OrderResultStatus.Reject) == 0,
				Address.BindableOrders.Changed());
			if (addressId != null)
				DisplayName = string.Format("Журнал отправки заказов для адреса {0}", Address.Name);
			else
				DisplayName = string.Format("Корректировка восстановленных заказов");
			ProductInfo = new ProductInfo(StatelessSession, Manager, Shell, CurrentLine);
			var view = GetView();
			if (view != null)
				Attach(view, ProductInfo.Bindings);
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			var query = Session.Query<OrderLine>()
				.Where(l => l.SendResult != LineResultStatus.OK || l.Order.SendResult != OrderResultStatus.OK);
			if (addressId != null) {
				query = query.Where(l => l.Order.Address.Id == addressId);
			}
			Lines = query.Distinct().ToList();
		}

		protected override void Query()
		{
			if (CurrentLine == null) {
				Offers.Value = new List<Offer>();
				return;
			}

			var productId = CurrentLine.Value.ProductId;
			Offers.Value = StatelessSession.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.ProductId == productId)
				.OrderBy(o => o.Cost)
				.ToList();
		}

		public IEnumerable<IResult> Save()
		{
			var save = new SaveFileResult(DisplayName);
			yield return save;
			save.Write(OrderLine.SendReport(Lines, groupByAddress: IsUpdate));
		}

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