using System.ComponentModel;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Inventory
{

	public class DefectusLine : BaseNotify, IEditableObject
	{
		public virtual uint Id { get; set; }

		public virtual string Product { get; set; }

		public virtual uint ProductId { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual string Producer { get; set; }

		public virtual uint? ProducerId { get; set; }

		public virtual uint Threshold { get; set; }

		public virtual uint OrderQuantity { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual void BeginEdit()
		{
		}

		public virtual void EndEdit()
		{
		}

		public virtual void CancelEdit()
		{
		}
	}
}