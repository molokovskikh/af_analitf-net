﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class TaskResult : IResult
	{
		public Task Task;
		WaitViewModel viewModel;

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
			Task.Start();
			Manager.ShowFixedDialog(viewModel);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
		public Extentions.WindowManager Manager;
	}
}