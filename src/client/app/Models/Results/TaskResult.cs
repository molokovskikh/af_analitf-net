using System;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Models.Results
{
	public class TaskResult : IResult
	{
		private log4net.ILog log = log4net.LogManager.GetLogger(typeof(TaskResult));
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
			Task.ContinueWith(t => {
				if (t.IsFaulted)
					log.Error("Выполнение задачи завершилось ошибкой", t.Exception);
				viewModel.IsCompleted = true;
				viewModel.TryClose();
			}, scheduler);
			Task.Start();
			Manager.ShowFixedDialog(viewModel);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
		public WindowManager Manager;
	}
}