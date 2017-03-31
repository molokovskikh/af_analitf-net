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
		public InputQuantity(OrderedStock stock)
		{
			InitFields();
			Quantity.Value = null;
			Multiplicity.Value = null;
			Stock.Value = stock;
			var env = Config.Env.Current;
			Warning = new InlineEditWarning(env.Scheduler, null);
			WasCancelled = true;
		}

		public InputQuantity(Stock stock)
		{
			InitFields();
			Quantity.Value = null;
			Multiplicity.Value = null;
			Stock.Value = Session.Connection
				.Query<OrderedStock>("select * from Stocks where Id = @Id", new { stock.Id })
				.First();
			var env = Config.Env.Current;
			Warning = new InlineEditWarning(env.Scheduler, null);
			WasCancelled = true;
		}

		public NotifyValue<uint?> Quantity { get; set; }
		public NotifyValue<uint?> Multiplicity { get; set; }
		public NotifyValue<OrderedStock> Stock { get; set; }
		public InlineEditWarning Warning { get; set; }
		public bool WasCancelled { get; set; }

		public void OK()
		{
			if (Quantity.Value == null)
			{
				Warning.Show(Common.Tools.Message.Warning($"Не указано количество"));
				return;
			}
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
				Warning.Show(Common.Tools.Message.Warning($"Заказ превышает количество в упаковке,"));
				return;
			}

			var doc = new UnpackingDoc(Address, User);
			var uline = new UnpackingLine(Stock.Value, (int)Multiplicity.Value);
			doc.Lines.Add(uline);
			doc.UpdateStat();
			doc.Post();
			Session.Save(doc);
			Session.Flush();

			Stock.Value = (OrderedStock)uline.DstStock;

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