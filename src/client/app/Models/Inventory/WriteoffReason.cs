using System.Collections.Generic;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class WriteoffReason : BaseStatelessObject
	{
		public WriteoffReason()
		{
		}

		public WriteoffReason(string name)
		{
			Name = name;
		}

		public override uint Id { get; set; }

		public virtual string Name { get; set; }

		public static WriteoffReason[] Default()
		{
			return new [] {
				new WriteoffReason("Недостача")
			};
		}
	}
}