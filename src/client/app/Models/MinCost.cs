using Common.Tools;

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

		public virtual decimal? Diff
		{
			get
			{
				return NullableHelper.Round(NextCost / Cost - 1 * 100, 2);
			}
		}
	}
}