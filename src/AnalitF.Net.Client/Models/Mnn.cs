namespace AnalitF.Net.Client.Models
{
	public class Mnn : BaseStatelessObject
	{
		public override uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual bool HaveOffers { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}