﻿using System;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public abstract class BaseOrderViewModel : BaseScreen
	{
		private bool isSendInit;

		public BaseOrderViewModel()
		{
			IsCurrentSelected = new NotifyValue<bool>(true);
			IsSentSelected = new NotifyValue<bool>();
			Begin = new NotifyValue<DateTime>(DateTime.Today);
			End = new NotifyValue<DateTime>(DateTime.Today);
			BeginEnabled = new NotifyValue<bool>(() => IsSentSelected, IsSentSelected);
			EndEnabled = new NotifyValue<bool>(() => IsSentSelected, IsSentSelected);
			Begin.Changed().Merge(End.Changed())
				.Subscribe(_ => Update());

			IsSentSelected.Changed()
				.Merge(IsCurrentSelected.Changed())
				.Skip(1)
				.Take(1)
				.Repeat()
				.Subscribe(n => {
					if (IsSentSelected && !isSendInit) {
						isSendInit = true;
						Update();
					}
				});
			IsCurrentSelected.Changed().Subscribe(_ => NotifyOfPropertyChange("CanPrint"));
			IsCurrentSelected.Changed().Subscribe(_ => NotifyOfPropertyChange("CanExport"));
		}

		public NotifyValue<bool> IsCurrentSelected { get; set ;}
		public NotifyValue<bool> IsSentSelected { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<bool> BeginEnabled { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> EndEnabled { get; set; }
	}
}