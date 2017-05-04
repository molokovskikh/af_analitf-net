using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AnalitF.Net.Client.ViewModels
{
	internal class PrintAsync : IResult
	{		
		private Func<IResult> syncAction;

		private PrintProgressViewModel viewModel;
		private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

		public PrintAsync(WindowManager windowManager, Func<IResult> syncAction)
		{
			this.viewModel = new PrintProgressViewModel(windowManager, cancellationTokenSource);
			this.syncAction = syncAction;
		}



		public void Execute(ActionExecutionContext context)
		{

			var newTask = new Task<IResult>(() =>
				{
#if DEBUG
					Thread.Sleep(25000);
#endif
					DispatcherOperation disOp = context.Source.Dispatcher.BeginInvoke(DispatcherPriority.Background,
						(Delegate)((Func<IResult>)(() =>
						{
							if (this.cancellationTokenSource.IsCancellationRequested)
								return null;
							return this.syncAction();
						}))
					);

					var dispatcherOperationStatus = disOp.Wait();
					return disOp.Result as IResult;
				},
				this.cancellationTokenSource.Token
			);

			newTask.ContinueWith(task =>
			{
				this.viewModel.Finished(); ;
				this.Completed?.BeginInvoke(
					context.Target,
					new ResultCompletionEventArgs() { WasCancelled = task.IsCanceled, Error = task.Exception },
					ar => { },
					task.Result
				);
			});

			this.viewModel.Start();
			newTask.Start();

		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}
