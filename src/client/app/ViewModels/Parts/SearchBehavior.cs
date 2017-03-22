using System;
using System.Reactive.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	//очередное безумие будь бдителен
	//BaseNotify не используется но если wpf binding используется для класса без INotifyPropertyChanged
	//этот объект попадет в глобальную таблицу внутри wpf и это приведет к утечки памяти
	public class SearchBehavior : BaseNotify, IDisposable
	{
		IDisposable dispose;
		//блокирует обработку ввода с клавиатуры в таблице что была возможность у другого обработчика
		//например у быстрого поиска
		public bool HandleGridKeyboardInput = true;

		public SearchBehavior(BaseScreen screen)
			: this(screen.Env)
		{
			screen.OnCloseDisposable.Add(this);
		}

		public SearchBehavior(Env env)
		{
			SearchText = new NotifyValue<string>();
			ActiveSearchTerm = new NotifyValue<string>();
			dispose = SearchText.Changed()
				.Throttle(Consts.SearchTimeout, env.Scheduler)
				.ObserveOn(env.UiScheduler)
				.Subscribe(_ => Search());
		}

		public NotifyValue<string> SearchText { get; set; }
		public NotifyValue<string> ActiveSearchTerm { get; set; }

		public IResult ClearSearch()
		{
			if (!String.IsNullOrEmpty(SearchText)) {
				SearchText.Value = "";
				return HandledResult.Handled();
			}

			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm.Value = "";
			SearchText.Value = "";
			return HandledResult.Handled();
		}

		public IResult Search()
		{
			var value = SearchText.Value;
			if (string.IsNullOrEmpty(value) || value.Length < 3)
				return HandledResult.Skip();

			//мы должны обнулить а затем записать что бы избежать срабатывания таймера
			SearchText.Value = "";
			ActiveSearchTerm.Value = value;
			return HandledResult.Handled();
		}

		public void Dispose()
		{
			dispose?.Dispose();
		}
	}
}