using System;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Controls;
using System.Windows.Documents;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models
{
	public class PrintResult : IResult
	{
		private string name;

		public PrintResult(FlowDocument doc, string name)
		{
			Doc = doc;
			Doc.PagePadding = new Thickness(25);
			Doc.ColumnGap = 0;
			Doc.ColumnWidth = double.PositiveInfinity;

			this.name = name;
		}

		public FlowDocument Doc { get; private set; }

		public void Execute(ActionExecutionContext context)
		{
			if (Doc == null)
				return;
			var dialog = new PrintDialog();
			if (dialog.ShowDialog() != true)
				return;

			var documentPaginator = ((IDocumentPaginatorSource)Doc).DocumentPaginator;
			documentPaginator = new WrapDocumentPaginator(documentPaginator);
			dialog.PrintDocument(documentPaginator, name);

			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}