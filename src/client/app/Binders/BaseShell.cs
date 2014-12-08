using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Binders
{
	public interface IActivateEx : IActivate
	{
		bool IsSuccessfulActivated { get; }
	}

	public class BaseShell : Conductor<IScreen>
	{
		public Navigator Navigator;

		public Env Env = new Env();
		public event Func<RemoteCommand, RemoteCommand> CommandExecuting;
		public Subject<IResult> ResultsSink = new Subject<IResult>();
		public CompositeDisposable CloseDisposable = new CompositeDisposable();
		public CancellationDisposable CancelDisposable = new CancellationDisposable();

		public BaseShell()
		{
			Navigator = new Navigator(this);
		}

		/// <summary>
		/// После загрузки формы нам нужно показать сообщения
		/// если это делать в обработчике то сообщение отобразится на фоне пустой формы
		/// тк форма еще не успеет нарисовать себя
		/// по этому планируем показ сообщений после того как форма будет нарисована
		/// </summary>
		protected override void OnViewLoaded(object view)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(
				DispatcherPriority.ContextIdle,
				new System.Action(() => Execute(OnViewReady())));
		}

		protected void Execute(IEnumerable<IResult> results)
		{
			RxHelper.ToObservable(results).Subscribe(v => ResultsSink.OnNext(v));
		}

		public virtual IEnumerable<IResult> OnViewReady()
		{
			return null;
		}

		public RemoteCommand OnCommandExecuting(RemoteCommand c)
		{
			if (CommandExecuting != null)
				return CommandExecuting(c) ?? c;
			return c;
		}

		protected override void ChangeActiveItem(IScreen newItem, bool closePrevious)
		{
			Configure(newItem);
			base.ChangeActiveItem(newItem, closePrevious);

			if (IsActive) {
				var activateEx = newItem as IActivateEx;
				if (activateEx != null && !activateEx.IsSuccessfulActivated) {
					this.CloseItem(ActiveItem);
				}
			}
		}

		protected virtual void Configure(IScreen newItem)
		{
		}

		protected void StartProcess(string exe, string args = "")
		{
			ProcessHelper.Start(new ProcessStartInfo(exe, args));
		}

		public override void TryClose(bool? dialogResult)
		{
#if DEBUG
			UnitTestClose();
#endif
			base.TryClose(dialogResult);
		}

		public override void TryClose()
		{
#if DEBUG
			UnitTestClose();
#endif
			base.TryClose();
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			if (close) {
				var disposable = this as IDisposable;
				if (disposable != null) {
					disposable.Dispose();
				}
			}
		}

#if DEBUG
		private void UnitTestClose()
		{
			if (Env.IsUnitTesting) {
				if (Parent == null && Views.Count == 0)
					CanClose(r => {
						if (r)
							ScreenExtensions.TryDeactivate(this, true);
					});
			}
		}
#endif

		public override void DeactivateItem(IScreen item, bool close)
		{
			if (item == null)
				return;
			CloseStrategy.Execute(new[] { item }, (canClose, closable) => {
				if (!canClose)
					return;
				if (item.Equals(ActiveItem))
					ChangeActiveItem(null, close);
				else
					ScreenExtensions.TryDeactivate(item, close);
			});

			if (close) {
				if (ActiveItem == null) {
					Navigator.NavigateBack();
				}
			}
		}
	}
}