namespace AnalitF.Net.Service.Models
{
	public class UserSettings
	{
		public virtual uint Id { get; set; }

		public virtual ulong WorkRegionMask { get; set; }

		public virtual bool AllowDownloadUnconfirmedOrders { get; set; }

		public virtual bool CheckClientToken { get; set; }
	}
}