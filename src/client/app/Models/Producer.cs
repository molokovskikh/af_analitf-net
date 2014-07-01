using System;

namespace AnalitF.Net.Client.Models
{
	public class Producer : BaseStatelessObject
	{
		public Producer()
		{
		}

		public Producer(uint id, string name)
		{
			Id = id;
			Name = name;
		}

		public Producer(string name)
		{
			Name = name;
		}

		public override uint Id { get; set; }
		public virtual string Name { get; set; }
		public virtual bool Hidden { get; set; }
	}
}