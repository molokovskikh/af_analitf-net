namespace AnalitF.Net.Client.Models
{
	public class MinCost
	{
		public virtual uint Id { get; set; }

		public virtual decimal Cost { get; set; }

		public virtual decimal? NextCost { get; set; }

		public virtual Catalog Catalog { get; set; }

		public virtual uint ProductId { get; set; }

		public virtual Price Price { get; set; }

		public virtual decimal? Diff { get; set; }

		public override string ToString()
		{
			return $"Cost: {Cost}, NextCost: {NextCost}, ProductId: {ProductId}, Diff: {Diff}";
		}
	}
}