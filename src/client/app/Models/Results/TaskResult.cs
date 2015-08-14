using System;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Models.Results
{
	public class TaskResult : IResult
	{
		public Task Task;
		WaitViewModel viewModel;

		public TaskResult(Task task)
		{
			this.Task = task;
			this.viewModel = new WaitViewModel();
		}

		public TaskResult(Task task, WaitViewModel viewModel)
		{
			this.Task = task;
			this.viewModel = viewModel;
		}

		public void Execute(ActionExecutionContext context)
		{
			TaskScheduler scheduler;
			if (SynchronizationContext.Current != null)
				scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			else
				scheduler = TaskScheduler.Current;
			Task.ContinueWith(t => {
				viewModel.IsCompleted = true;
				viewModel.TryClose();
			}, scheduler);
			if (Task.Status == TaskStatus.Created)
				Task.Start();
			Manager.ShowFixedDialog(viewModel);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
		public WindowManager Manager;
	}
}