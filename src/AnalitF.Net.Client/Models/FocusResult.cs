using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models
{
	public class FocusResult : IResult
	{
		private string name;

		public FocusResult(string name)
		{
			this.name = name;
		}

		public void Execute(ActionExecutionContext context)
		{
			var element = context.View.DeepChildren().OfType<FrameworkElement>().First(o => o.Name == name);
			if (element is DataGrid)
				DataGridHelper.Focus((DataGrid)element);
			else
				element.Focus();

			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}