using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Dapper;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class OrderedStock : Stock, IInlineEditable
	{
		private uint? _ordered;

		public uint? Ordered
		{
			get { return _ordered; }
			set
			{
				if (_ordered != value) {
					_ordered = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(OrderedSum));
				}
			}
		}

		public decimal? OrderedSum => RetailCost * Ordered;

		public uint Value
		{
			get { return Ordered.GetValueOrDefault(); }
			set { Ordered = value > 0 ? (uint?)value : null; }
		}
	}

	public class StockChooser : Screen, ICancelable, IEditor
	{
		public StockChooser(uint catalogId, IList<CheckLine> lines, Address address)
		{
			BaseScreen.InitFields(this);
			DisplayName = "¬ыберете товар";
			IsLoading.Value = true;
			var env = Config.Env.Current;
			Warning = new InlineEditWarning(env.Scheduler, null);

			env.RxQuery(s => s.Get<Catalog>(catalogId))
				.Subscribe(x => Name = x?.FullName);
			env.RxQuery(s => {
				var sql = "select * from Stocks where CatalogId = @catalogId and AddressId = @addressId and Quantity > 0";
				var items = s.Connection.Query<OrderedStock>(sql, new { catalogId, addressId = address.Id })
					.OrderBy(x => x.Exp)
					.ToList();
				foreach (var item in items) {
					if (item.Exp != null)
						item.Exp = item.Exp.Value.ToLocalTime();
					item.Ordered = (uint?)lines.FirstOrDefault(x => x.Id == item.Id)?.Quantity;
				}
				return items;
			}).Do(_ => IsLoading.Value = false).Subscribe(Items);
		}

		public string Name { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }
		public NotifyValue<List<OrderedStock>> Items { get; set;}
		public NotifyValue<OrderedStock> CurrentItem { get; set; }
		public InlineEditWarning Warning { get; set; }
		public bool WasCancelled { get; set; }

		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			if (Items.Value.All(x => x.Ordered == null)) {
				CurrentItem.Value.Ordered = 1;
				Updated();
			}
			WasCancelled = false;
			TryClose();
		}

		public void Updated()
		{
			if (CurrentItem.Value.Ordered > CurrentItem.Value.Quantity) {
				Warning.Show(Common.Tools.Message.Warning($"«аказ превышает остаток на складе, товар будет заказан в количестве {CurrentItem.Value.Quantity}"));
				CurrentItem.Value.Ordered = (uint)CurrentItem.Value.Quantity;
			}
		}

		public void Committed()
		{
		}

		public override void TryClose()
		{
			Committed();
			base.TryClose();
		}
	}
}