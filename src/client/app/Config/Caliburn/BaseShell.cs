using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Windows.Controls;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public interface IActivateEx : IActivate
	{
		bool IsSuccessfulActivated { get; }
	}

	public class BaseShell : Conductor<IScreen>.Collection.OneActive
	{
		public INavigator Navigator;

		public Env Env = Env.Current ?? new Env();
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

		protected void StartProcess(string exe, string args = "", string workDir = null)
		{
			var info = new ProcessStartInfo(exe, args);
			if (!String.IsNullOrEmpty(workDir))
				info.WorkingDirectory = workDir;
			ProcessHelper.Start(info);
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
				(this as IDisposable)?.Dispose();
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
			if (close) {
				//исправление для ошибки
				//Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.TabControl', AncestorLevel='1''. BindingExpression:Path=TabStripPlacement; DataItem=null; target element is 'TabItem' (Name=''); target property is 'NoTarget' (type 'Object')
				var view = (ShellView)GetView();
				if (view != null)
				{
					var tabs = view.Items;
					var tab = (TabItem)tabs.ItemContainerGenerator.ContainerFromItem(item);
					if (tab != null)
						tab.Template = null;
				}
			}

			base.DeactivateItem(item, close);

			if (close) {
				Navigator.Release(item);
			}
		}
	}
}