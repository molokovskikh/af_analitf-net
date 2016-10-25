using System;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Common.Tools.Calendar;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public abstract class BaseOrderViewModel : BaseScreen
	{
		public BaseOrderViewModel()
		{
			IsCurrentSelected = new NotifyValue<bool>(true);
			IsSentSelected = new NotifyValue<bool>();
			IsDeletedSelected = new NotifyValue<bool>();
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3).FirstDayOfMonth());
			End = new NotifyValue<DateTime>(DateTime.Today);
			BeginEnabled = IsSentSelected.ToValue();
			EndEnabled = IsSentSelected.ToValue();

			IsCurrentSelected.Subscribe(_ => NotifyOfPropertyChange("CanPrint"));
			IsCurrentSelected.Subscribe(_ => NotifyOfPropertyChange(nameof(CanExport)));
			SessionValue(Begin, "Begin");
			SessionValue(End, "End");
		}

		public NotifyValue<bool> IsCurrentSelected { get; set ;}
		public NotifyValue<bool> IsSentSelected { get; set; }
		public NotifyValue<bool> IsDeletedSelected { get; set; }

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<bool> BeginEnabled { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> EndEnabled { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Begin.Merge(End).Skip(2).CatchSubscribe(_ => Update(), CloseCancellation);
			IsSentSelected.Merge(IsDeletedSelected).Merge(IsCurrentSelected)
				.Where(_ => IsCurrentSelected ^ IsSentSelected.Value ^ IsDeletedSelected.Value)
				.CatchSubscribe(_ => Update(), CloseCancellation);
		}
	}
}