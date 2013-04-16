using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	[DataContract]
	public class AddressSelector : ViewAware
	{
		public AddressSelector(ISession session, IScheduler scheduler, BaseOrderViewModel screen)
		{
			All = new NotifyValue<bool>();
			AddressesEnabled = new NotifyValue<bool>(() => All.Value, All);
			Addresses = session.Query<Address>()
				.OrderBy(a => a.Name)
				.Select(a => new Selectable<Address>(a)).ToList();
			Addresses.Select(a => Observable.FromEventPattern<PropertyChangedEventArgs>(a, "PropertyChanged"))
				.Merge()
				.Throttle(Consts.FilterUpdateTimeout, scheduler)
				.Subscribe(_ => screen.Update());
		}

		[DataMember]
		public NotifyValue<bool> All { get; set; }

		public bool AllVisible
		{
			get { return Addresses.Count > 1; }
		}

		public IList<Selectable<Address>> Addresses { get; set; }

		public bool AddressesVisible
		{
			get { return Addresses.Count > 1; }
		}

		public NotifyValue<bool> AddressesEnabled { get; set; }
	}
}