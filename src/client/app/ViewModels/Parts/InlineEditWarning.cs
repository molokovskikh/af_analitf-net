using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Caliburn.Micro;
using Common.Tools;
using ReactiveUI;
using Message = Common.Tools.Message;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class InlineEditWarning : ViewAware
	{
		private string _orderWarning;
		private Extentions.WindowManager _manager;

		public InlineEditWarning(IScheduler scheduler, Extentions.WindowManager manager)
		{
			_manager = manager;

			this.ObservableForProperty(m => m.OrderWarning)
				.Where(m => !String.IsNullOrEmpty(m.Value))
				.Throttle(Consts.WarningTimeout)
				.ObserveOn(scheduler)
				.Subscribe(m => { OrderWarning = null; });
		}

		public string OrderWarning
		{
			get { return _orderWarning; }
			set
			{
				_orderWarning = value;
				NotifyOfPropertyChange("OrderWarning");
			}
		}

		public void Show(List<Message> messages)
		{
			var warnings = messages.Where(m => m.IsWarning).Implode(Environment.NewLine);
			//нельзя перетирать старые предупреждения, предупреждения очищаются только по таймеру
			if (!String.IsNullOrEmpty(warnings))
				OrderWarning = warnings;

			var errors = messages.Where(m => m.IsError);
			foreach (var message in errors) {
				_manager.Warning(message.MessageText);
			}
		}

		public override string ToString()
		{
			return OrderWarning;
		}
	}
}