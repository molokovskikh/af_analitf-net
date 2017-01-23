namespace AnalitF.Net.Service.Models
{
	public class ClientSettings
	{
		public virtual uint Id { get; set; }

		public virtual bool ShowAdvertising { get; set; }

		public virtual bool AllowDelayOfPayment { get; set; }

		public virtual bool AllowAnalitFSchedule { get; set; }

		public virtual bool IsStockEnabled { get; set; }
	}
}