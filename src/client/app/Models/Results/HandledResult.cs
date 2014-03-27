using System;
using System.Reactive;
using System.Windows.Input;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class HandledResult : IResult
	{
		private bool handled;

		public HandledResult(bool handled = true)
		{
			this.handled = handled;
		}

		public void Execute(ActionExecutionContext context)
		{
			var args = ExtractKeyEventArgs(context);

			if (args != null && handled) {
				args.Handled = handled;
			}
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public static KeyEventArgs ExtractKeyEventArgs(ActionExecutionContext context)
		{
			var reactiveEvent = context.EventArgs as EventPattern<KeyEventArgs>;
			var args = context.EventArgs as KeyEventArgs;
			if (args == null && reactiveEvent != null)
				args = reactiveEvent.EventArgs;
			return args;
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public static HandledResult Skip()
		{
			return new HandledResult(false);
		}

		public static HandledResult Handled()
		{
			return new HandledResult();
		}
	}
}