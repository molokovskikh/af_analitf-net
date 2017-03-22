using System;
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
			BaseScreen.InitFields(this);
			Amount.Value = sum;
			Amount.CombineLatest(CardAmount, (x, y) => Abs(x + y.GetValueOrDefault() - Sum)).Subscribe(Change);
			Amount.CombineLatest(CardAmount, (x, y) => x.GetValueOrDefault() + y.GetValueOrDefault() >= sum
				&& y.GetValueOrDefault() <= sum
				&& !(x.GetValueOrDefault() > sum && y.GetValueOrDefault() > 0))
				.Subscribe(IsValid);
			DisplayName = "Введите сумму оплаты";
		}

		public bool WasCancelled { get; private set; }
		public decimal Sum { get; set; }
		public NotifyValue<decimal?> Change { get; set; }
		public NotifyValue<decimal?> Amount { get; set; }
		public NotifyValue<decimal?> CardAmount { get; set; }
		public NotifyValue<bool> IsValid { get; set; }

		public decimal? Abs(decimal? value)
		{
			if (value == null)
				return null;
			return Math.Abs(value.Value);
		}

		public void OK()
		{
			if (!IsValid) {
				return;
			}
			WasCancelled = false;
			TryClose();
		}
	}
}