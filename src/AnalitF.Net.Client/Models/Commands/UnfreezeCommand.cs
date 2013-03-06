namespace AnalitF.Net.Client.Models.Commands
{
	public class UnfreezeCommand : DbCommand
	{
		private uint id;

		public UnfreezeCommand(uint id)
		{
			this.id = id;
		}

		public override void Execute()
		{
			var order = Session.Load<Order>(id);
			var newOrder = order.Unfreeze(Session);

			if (order.IsEmpty)
				Session.Delete(order);

			if (newOrder != null && !newOrder.IsEmpty)
				Session.Save(newOrder);
		}
	}
}