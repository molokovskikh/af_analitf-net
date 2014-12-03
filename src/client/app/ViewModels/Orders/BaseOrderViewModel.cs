using System;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	public abstract class BaseOrderViewModel : BaseScreen
	{
		public BaseOrderViewModel()
		{
			IsCurrentSelected = new NotifyValue<bool>(true);
			IsSentSelected = new NotifyValue<bool>();
			Begin = new NotifyValue<DateTime>(DateTime.Today);
			End = new NotifyValue<DateTime>(DateTime.Today);
			BeginEnabled = IsSentSelected.ToValue();
			EndEnabled = IsSentSelected.ToValue();

			IsCurrentSelected.Subscribe(_ => NotifyOfPropertyChange("CanPrint"));
			IsCurrentSelected.Subscribe(_ => NotifyOfPropertyChange("CanExport"));
		}

		public NotifyValue<bool> IsCurrentSelected { get; set ;}
		public NotifyValue<bool> IsSentSelected { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<bool> BeginEnabled { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> EndEnabled { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			var beginValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".Begin");
			if (beginValue is DateTime)
				Begin.Value = (DateTime)beginValue;
			var endValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".End");
			if (endValue is DateTime)
				End.Value = (DateTime)endValue;
			Begin.Merge(End).Skip(2).CatchSubscribe(_ => Update(), CloseCancellation);
			IsSentSelected.Merge(IsCurrentSelected).Skip(2)
				.Where(_ => !(IsSentSelected.Value && IsCurrentSelected.Value))
				.CatchSubscribe(_ => Update(), CloseCancellation);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			Shell.SessionContext[GetType().Name + ".Begin"] = Begin.Value;
			Shell.SessionContext[GetType().Name + ".End"] = End.Value;
		}
	}
}