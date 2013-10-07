﻿using System;
using Caliburn.Micro;

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

		public void Execute(ActionExecutionContext context)
		{
			var manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
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
	}
}