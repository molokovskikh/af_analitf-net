using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public abstract class DbCommand
	{
		public ISession Session;

		public abstract void Execute();
	}

	public class ReorderCommand : DbCommand
	{
		private uint id;

		public ReorderCommand(uint id)
		{
			this.id = id;
		}

		public override void Execute()
		{
			var order = Session.Load<Order>(id);
			var orders = Session.Query<Order>().Where(o => o.Address == order.Address
				&& !o.Frozen
				&& o.Id != order.Id)
				.ToArray();
			var priceIds = orders.Select(o => o.Price.Id.PriceId).ToArray();
			var regionIds = orders.Select(o => o.Price.Id.RegionId).ToArray();
			var productIds = order.Lines.Select(l => l.ProductId).ToArray();
			var offers = Session.Query<Offer>()
				.Where(o => priceIds.Contains(o.Price.Id.PriceId)
					&& regionIds.Contains(o.Price.Id.RegionId)
					&& productIds.Contains(o.ProductId))
				.ToArray();
			order.Reorder(orders, offers);

			if (order.IsEmpty)
				Session.Delete(order);
		}
	}
}