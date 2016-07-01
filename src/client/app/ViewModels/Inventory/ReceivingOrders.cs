using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReceivingOrders : BaseScreen2
	{
		private Main main;

		public ReceivingOrders(Main main)
		{
			this.main = main;
			Statuses = new [] {
				new Selectable<ValueDescription>(new ValueDescription(ReceiveStatus.New)),
				new Selectable<ValueDescription>(new ValueDescription(ReceiveStatus.Closed)),
				new Selectable<ValueDescription>(new ValueDescription(ReceiveStatus.InProgress)),
			};
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
		}

		public NotifyValue<List<ReceivingOrder>> Items { get; set; }
		public NotifyValue<ReceivingOrder> CurrentItem { get; set; }
		public NotifyValue<IList<Selectable<Supplier>>> Suppliers { get; set; }
		public NotifyValue<IList<Selectable<Address>>> AddressesFilter { get; set; }
		public IList<Selectable<ValueDescription>> Statuses { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(x => x.Query<Supplier>().OrderBy(y => y.Name).ToArray().Select(y => new Selectable<Supplier>(y)).ToList())
				.Subscribe(Suppliers);
			RxQuery(x => x.Query<Address>().OrderBy(y => y.Name).ToArray().Select(y => new Selectable<Address>(y)).ToList())
				.Subscribe(AddressesFilter);

			RxQuery(x => x.Query<ReceivingOrder>()
					.Fetch(y => y.Supplier)
					.Fetch(y => y.Address)
					.OrderByDescending(y => y.OrderDate).ToList())
				.Subscribe(Items);
		}

		public void Create()
		{
			throw new NotImplementedException();
		}

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			main.ActiveItem = new ReceivingDetails(CurrentItem.Value.Id);
		}
	}
}