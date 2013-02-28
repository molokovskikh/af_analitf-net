using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Common.Tools;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class InlineEditWarningViewModel : INotifyPropertyChanged
	{
		private string _orderWarning;
		private Extentions.WindowManager _manager;

		public InlineEditWarningViewModel(IScheduler scheduler, Extentions.WindowManager manager)
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
				OnPropertyChanged("OrderWarning");
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

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public override string ToString()
		{
			return OrderWarning;
		}
	}
}