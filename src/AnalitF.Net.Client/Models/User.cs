namespace AnalitF.Net.Client.Models
{
	public class User
	{
		public virtual uint Id { get; set; }

		public virtual string FullName { get; set; }

		public virtual bool IsPriceEditDisabled { get; set; }
	}
}