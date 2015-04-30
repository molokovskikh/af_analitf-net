using System;
using System.Collections.Generic;
using System.Windows;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Models.Results
{
	public interface ICancelable
	{
		bool WasCancelled { get; }
	}

	public class WindowResult : IResult
	{
		private Screen model;
		public IWindowManager Manager;

		public WindowResult(Screen model)
		{
			this.model = model;
		}

		public void Execute(ActionExecutionContext context)
		{
			Manager.ShowWindow(model);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}

	public class DialogResult : IResult
	{
		public Screen Model;
		public bool ShowSizeToContent;
		public bool FullScreen;

		public DialogResult(Screen model, bool fullScreen = false, bool sizeToContent = false)
		{
			Model = model;
			FullScreen = fullScreen;
			ShowSizeToContent = sizeToContent;
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public void Execute(ActionExecutionContext context)
		{
			var manager = (WindowManager)IoC.Get<IWindowManager>();
			var args = new ResultCompletionEventArgs();
			IDictionary<string, object> settings = null;
			if (FullScreen) {
				settings = new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				};
			}

			if (ShowSizeToContent)
				manager.ShowFixedDialog(Model);
			else
				manager.ShowDialog(Model, null, settings);

			var cancellable = Model as ICancelable;
			if (cancellable != null)
				args.WasCancelled = cancellable.WasCancelled;
			RaiseCompleted(args);
		}

		public void RaiseCompleted(ResultCompletionEventArgs args)
		{
			if (Completed != null)
				Completed(this, args);
		}

		public override string ToString()
		{
			return String.Format("Диалог - {0}", Model);
		}
	}
}