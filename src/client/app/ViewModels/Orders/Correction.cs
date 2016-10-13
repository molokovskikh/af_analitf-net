using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
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

		public bool IsOrderSend => addressId != null;

		public bool IsUpdate => addressId == null;

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (IsUpdate)
				CurrentLine
					.Throttle(Consts.ScrollLoadTimeout)
					.SelectMany(x => Env.RxQuery(s => {
						if (x == null) {
							return new List<Offer>();
						}

						var productId = x.ProductId;
						return s.Query<Offer>()
							.Fetch(o => o.Price)
							.Where(o => o.ProductId == productId)
							.ToList()
							.OrderBy(o => o.ResultCost)
							.ToList();
					}))
					.Subscribe(UpdateOffers);

			CanSend = Address.BindableOrders.Changed()
				.Merge(Observable.Return<object>(null))
				.ToValue(_ => Address.Orders.Any(o => o.Send)
					&& Address.Orders.Count(o => o.Send && o.SendResult == OrderResultStatus.Reject) == 0);

			if (addressId != null)
				DisplayName = $"Журнал отправки заказов для адреса {Address.Name}";
			else
				DisplayName = "Корректировка восстановленных заказов";
			ProductInfo = new ProductInfo(this, CurrentLine);
			Attach(GetView(), ProductInfo.Bindings);
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			var query = Session.Query<OrderLine>()
				.Where(l => l.SendResult != LineResultStatus.OK || l.Order.SendResult != OrderResultStatus.OK);
			if (addressId != null) {
				query = query.Where(l => l.Order.Address.Id == addressId);
			}
			var lines = query.Distinct().ToList();
			lines.Each(l => l.Configure(User));
			Lines = lines;
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