using System;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Models.Results
{
	public class TaskResult : IResult
	{
		public static log4net.ILog log = log4net.LogManager.GetLogger(typeof(TaskResult));

		private WaitViewModel viewModel;
		public Task Task;

		public TaskResult(Task task, WaitViewModel viewModel = null)
		{
			this.Task = task;
			this.viewModel = viewModel ?? new WaitViewModel();
		}

		public void Execute(ActionExecutionContext context)
		{
			TaskScheduler scheduler;
			if (SynchronizationContext.Current != null)
				scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			else
				scheduler = TaskScheduler.Current;
			var args = new ResultCompletionEventArgs();
			Task.ContinueWith(t => {
				if (t.IsFaulted)
					args.Error = t.Exception;
				if (t.IsCanceled) {
					log.Warn($"Отменена задача {viewModel.DisplayName}");
					args.WasCancelled = t.IsCanceled;
				}
				viewModel.IsCompleted = true;
				viewModel.TryClose();
				if (t.IsFaulted)
					Manager.Error(ErrorHelper.TranslateException(t.Exception) ?? viewModel.GenericErrorMessage);
			}, scheduler);
			if (Task.Status == TaskStatus.Created)
				Task.Start(TaskScheduler.Default);
			Manager.ShowFixedDialog(viewModel);
			Completed?.Invoke(this, args);
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
		public WindowManager Manager;
	}
}