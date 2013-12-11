using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Xps;

namespace AnalitF.Net.Client.Controls
{
	public class DocumentViewer2 : DocumentViewer
	{
		public static DependencyProperty OrientationProperty
			= DependencyProperty.RegisterAttached("Orientation",
				typeof(PageOrientation),
				typeof(DocumentViewer2),
				new FrameworkPropertyMetadata(PageOrientation.Unknown));

		private XpsDocumentWriter _writer;

		static DocumentViewer2()
		{
			CommandManager.RegisterClassCommandBinding(typeof(DocumentViewer2),
				new CommandBinding(ApplicationCommands.Print, Execute, CanExecute));
			CommandManager.RegisterClassInputBinding(typeof(DocumentViewer2),
				new InputBinding(ApplicationCommands.Print, new KeyGesture(Key.P, ModifierKeys.Control)));
			CommandManager.RegisterClassCommandBinding(typeof(DocumentViewer2),
				new CommandBinding(ApplicationCommands.CancelPrint, Execute, CanExecute));
		}

		public PageOrientation Orientation
		{
			get { return (PageOrientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		private static void CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var doc = sender as DocumentViewer2;
			if (e.Command == ApplicationCommands.Print) {
				e.CanExecute = doc.Document != null && doc._writer == null;
				e.Handled = true;
			}
			else if (e.Command == ApplicationCommands.CancelPrint)
				e.CanExecute = doc._writer != null;
			else
				e.CanExecute = true;
		}

		private static void Execute(object sender, ExecutedRoutedEventArgs e)
		{
			var doc = sender as DocumentViewer2;
			if (e.Command == ApplicationCommands.Print)
				doc.OnPrintCommand();
			else if (e.Command == ApplicationCommands.CancelPrint)
				doc.OnCancelPrintCommand();
		}

		protected override void OnPrintCommand()
		{
			if (Document == null || _writer != null)
				return;

			var dialog = new PrintDialog();
			if (dialog.ShowDialog() != true)
				return;

			if (Orientation != PageOrientation.Unknown)
				dialog.PrintTicket.PageOrientation = Orientation;

			_writer = PrintQueue.CreateXpsDocumentWriter(dialog.PrintQueue);
			_writer.WritingPrintTicketRequired += (sender, args) => {
				args.CurrentPrintTicket = dialog.PrintTicket;
			};
			_writer.WritingCompleted += Completed;
			_writer.WritingCancelled += Cancelled;
			CommandManager.InvalidateRequerySuggested();
			if (Document is FixedDocumentSequence)
				_writer.WriteAsync(Document as FixedDocumentSequence);
			else if (Document is FixedDocument)
				_writer.WriteAsync(Document as FixedDocument);
			else
				_writer.WriteAsync(Document.DocumentPaginator);
		}

		private void Cancelled(object sender, WritingCancelledEventArgs e)
		{
			CleanUpWriter();
		}

		private void Completed(object sender, WritingCompletedEventArgs e)
		{
			CleanUpWriter();
		}

		private void CleanUpWriter()
		{
			if (_writer == null)
				return;

			_writer.WritingCancelled -= Cancelled;
			_writer.WritingCompleted -= Completed;
			_writer = null;
			CommandManager.InvalidateRequerySuggested();
		}
	}
}