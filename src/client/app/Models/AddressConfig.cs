namespace AnalitF.Net.Client.Models
{
	public class AddressConfig
	{
		public AddressConfig()
		{
		}

		public AddressConfig(Address x)
		{
			Address = x;
			IsActive = true;
		}

		public virtual uint Id { get; set; }
		public virtual Address Address { get; set; }
		public virtual bool IsActive { get; set; }
	}
}