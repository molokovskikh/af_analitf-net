using System;
using Caliburn.Micro;
using WindowManager = AnalitF.Net.Client.Config.Caliburn.WindowManager;

namespace AnalitF.Net.Client.Models.Results
{
	public class MessageResult : IResult
	{
		public enum MessageType
		{
			Notify,
			Warning,
			Error
		}

		public string Message;
		public MessageType Type;

		public MessageResult(string message, MessageType type = MessageType.Notify)
		{
			Message = message;
			Type = type;
		}

		public static MessageResult Warn(string message)
		{
			return new MessageResult(message, MessageType.Warning);
		}

		public static MessageResult Error(string message)
		{
			return new MessageResult(message, MessageType.Error);
		}

		public static MessageResult Error(string message, params object[] args)
		{
			return new MessageResult(String.Format(message, args), MessageType.Error);
		}

		public void Execute(ActionExecutionContext context)
		{
			var manager = (WindowManager)IoC.Get<IWindowManager>();
			switch (Type) {
				case MessageType.Error:
					manager.Error(Message);
					break;
				case MessageType.Notify:
					manager.Notify(Message);
					break;
				case MessageType.Warning:
					manager.Warning(Message);
					break;
			}
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public override string ToString()
		{
			return Message;
		}
	}
}