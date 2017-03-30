using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Models.Inventory;
using Dapper;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class InputQuantity : BaseScreen
	{
		public InputQuantity(OrderedStock stock, bool unpackingVisible)
		{
			InitFields();
			Quantity.Value = 1;
			Multiplicity.Value = null;
			Unpacking.Value = false;
			Stock.Value = stock;
			UnpackingVisible.Value = unpackingVisible;
			Unpacking.Value = !unpackingVisible;
			var env = Config.Env.Current;
			Warning = new InlineEditWarning(env.Scheduler, null);
		}

		public InputQuantity(Stock stock, bool unpackingVisible)
		{
			InitFields();
			Quantity.Value = 1;
			Multiplicity.Value = null;
			Unpacking.Value = false;
			Stock.Value = Session.Connection
				.Query<OrderedStock>("select * from Stocks where Id = @Id", new { stock.Id })
				.First();
			UnpackingVisible.Value = unpackingVisible;
			Unpacking.Value = !unpackingVisible;
			var env = Config.Env.Current;
			Warning = new InlineEditWarning(env.Scheduler, null);
		}

		public NotifyValue<uint> Quantity { get; set; }
		public NotifyValue<uint?> Multiplicity { get; set; }
		public NotifyValue<bool> Unpacking { get; set; }
		public NotifyValue<bool> UnpackingVisible { get; set; }
		public NotifyValue<OrderedStock> Stock { get; set; }
		public InlineEditWarning Warning { get; set; }
		public bool WasCancelled { get; set; }

		public void OK()
		{
			if (Unpacking)
			{
				if (Multiplicity.Value == null)
				{
					Warning.Show(Common.Tools.Message.Warning($"Не указано количество в упаковке"));
					return;
				}
				if (Stock.Value.Unpacked)
				{
					Warning.Show(Common.Tools.Message.Warning($"Данная партия уже распакована"));
					return;
				}
				if (Quantity.Value > Multiplicity.Value)
				{
					Warning.Show(Common.Tools.Message.Warning($"Заказ превышает количества в упаковке," +
						$"\nтовар будет заказан в количестве {Multiplicity.Value}"));
					Quantity.Value = (uint)Multiplicity.Value;
				}

				var doc = new UnpackingDoc(Address);
				var uline = new UnpackingLine(Stock.Value, (int)Multiplicity.Value);
				doc.Lines.Add(uline);
				doc.UpdateStat();
				doc.Post();
				Session.Save(doc);
				Session.Flush();

				Stock.Value = (OrderedStock)uline.DstStock;
			}
			else
			{
				var stockQuantity = Stock.Value.Quantity;
				if (Quantity.Value > stockQuantity)
				{
					Warning.Show(Common.Tools.Message.Warning($"Заказ превышает остаток на складе," +
						$"\nтовар будет заказан в количестве {stockQuantity}"));
					Quantity.Value = (uint)stockQuantity;
				}
			}
			Stock.Value.Ordered = Quantity.Value;
			WasCancelled = false;
			TryClose();
		}

		public void Committed()
		{
		}

		public override void TryClose()
		{

			Committed();
			base.TryClose();
		}
	}
}