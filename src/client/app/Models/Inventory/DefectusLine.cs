using System.Collections;
using System.ComponentModel;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Inventory
{

	public class DefectusLine : BaseNotify, IEditableObject
	{
		private Hashtable props = null;

		[Ignore]
		public virtual bool IsDirty { get; set; }

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
			// сохранили первоначальное состояние до всех редактирований
			if (props != null)
				return;
			props = new Hashtable(2);
			props.Add("Threshold", Threshold);
			props.Add("OrderQuantity", OrderQuantity);
		}

		public virtual void EndEdit()
		{
			// помечаем только если состояние изменилось относительно первоначального
			if (Threshold == (uint)props["Threshold"] && OrderQuantity == (uint)props["OrderQuantity"])
				IsDirty = false;
			else
				IsDirty = true;
		}

		public virtual void CancelEdit()
		{
		}
	}
}