using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Windows.Threading;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public interface IActivateEx : IActivate
	{
		bool IsSuccessfulActivated { get; }
	}

	public class BaseShell : Conductor<IScreen>
	{
		public Navigator Navigator;

		public bool UnitTesting;
		public Env Env = new Env();
		public event Func<RemoteCommand, RemoteCommand> CommandExecuting;

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
				new System.Action(OnViewReady));
		}

		public virtual void OnViewReady()
		{
		}

		public RemoteCommand OnCommandExecuting(RemoteCommand c)
		{
			if (CommandExecuting != null)
				return CommandExecuting(c) ?? c;
			return c;
		}

		protected override void ChangeActiveItem(IScreen newItem, bool closePrevious)
		{
			var baseScreen = newItem as BaseScreen;
			if (baseScreen != null) {
				baseScreen.Env = Env;
			}

			base.ChangeActiveItem(newItem, closePrevious);

			if (IsActive) {
				var activateEx = newItem as IActivateEx;
				if (activateEx != null && !activateEx.IsSuccessfulActivated) {
					this.CloseItem(ActiveItem);
				}
			}
		}

		protected void StartProcess(string exe, string args = "")
		{
			ProcessHelper.Start(new ProcessStartInfo(exe, args));
		}

		public override void TryClose(bool? dialogResult)
		{
			UnitTestClose();
			base.TryClose(dialogResult);
		}

		public override void TryClose()
		{
			UnitTestClose();
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

		private void UnitTestClose()
		{
			if (UnitTesting) {
				if (Parent == null && Views.Count == 0)
					CanClose(r => {
						if (r)
							ScreenExtensions.TryDeactivate(this, true);
					});
			}
		}

		public override void DeactivateItem(IScreen item, bool close)
		{
			base.DeactivateItem(item, close);

			if (close) {
				if (ActiveItem == null) {
					Navigator.NavigateBack();
				}
			}
		}
	}
}