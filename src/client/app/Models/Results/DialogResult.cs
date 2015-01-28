using System;
using System.Collections.Generic;
using System.Windows;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public interface ICancelable
	{
		bool WasCancelled { get; }
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
			var manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
			var args = new ResultCompletionEventArgs();
			IDictionary<string, object> settings = null;
			if (FullScreen) {
				settings = new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				};
			}

			//по умолчанию ShowDialog вернет false те по умолчанию мы продолжаем выполение
			//что бы отменить выполение цепочки нужно явно сказать TryClose(true)
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