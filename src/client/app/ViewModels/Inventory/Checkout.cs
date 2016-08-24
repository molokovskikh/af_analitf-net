using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Checkout : Screen, ICancelable
	{
		public Checkout(decimal sum)
		{
			Sum = sum;
			WasCancelled = true;
			Change = new NotifyValue<decimal?>();
			Amount = new NotifyValue<decimal?>(sum);
			Amount.Select(x => x - Sum).Subscribe(Change);
			DisplayName = "Введите сумму оплаты";
		}

		public bool WasCancelled { get; private set; }
		public decimal Sum { get; set; }
		public NotifyValue<decimal?> Change { get; set; }
		public NotifyValue<decimal?> Amount { get; set; }

		public void OK()
		{
			if (Amount.Value.GetValueOrDefault() < Sum) {
				return;
			}
			WasCancelled = false;
			TryClose();
		}
	}
}