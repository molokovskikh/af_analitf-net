using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class AddressSelector : ViewAware
	{
		private BaseScreen screen;

		public IObservable<EventPattern<PropertyChangedEventArgs>> FilterChanged;

		public AddressSelector(ISession session, BaseScreen screen)
		{
			this.screen = screen;
			All = new NotifyValue<bool>();
			AddressesEnabled = new NotifyValue<bool>(() => All.Value, All);
			if (session != null)
				Addresses = session.Query<Address>()
					.OrderBy(a => a.Name)
					.Select(a => new Selectable<Address>(a)).ToList();
			else
				Addresses = new List<Selectable<Address>>();

			Description = "Все заказы";
		}

		public string Description { get; set; }

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

		public void Init()
		{
			var shell = screen.Shell;
			if (shell != null) {
				All.Value = (bool)shell.SessionContext.GetValueOrDefault("ShowAllAddresses", All.Value);
				var selectedAddresses = (IDictionary<uint, bool>)shell.SessionContext.GetValueOrDefault("SelectedAddresses", new Dictionary<uint, bool>());
				Addresses.Each(a => a.IsSelected = selectedAddresses.GetValueOrDefault(a.Item.Id, true));
			}
			FilterChanged = Addresses.Select(a => a.Changed())
				.Merge()
				.Throttle(Consts.FilterUpdateTimeout, screen.UiScheduler)
				.Merge(All.Changed());
		}

		public void Deinit()
		{
			var shell = screen.Shell;
			if (shell != null) {
				shell.SessionContext["ShowAllAddresses"] = All.Value;
				shell.SessionContext["SelectedAddresses"] = Addresses.ToDictionary(a => a.Item.Id, a => a.IsSelected);
			}
		}

		public Address[] GetActiveFilter()
		{
			var filterAddresses = new Address[0];
			if (All.Value) {
				filterAddresses = Addresses
					.Where(a => a.IsSelected)
					.Select(a => a.Item)
					.ToArray();
			}
			else if (screen.Address != null) {
				filterAddresses = new[] { screen.Address };
			}
			return filterAddresses;
		}
	}
}