using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Xps;
using AnalitF.Net.Client.Models.Results;
using log4net;

namespace AnalitF.Net.Client.Controls
{
	public class DocumentViewer2 : DocumentViewer
	{
		private ILog log = LogManager.GetLogger(typeof(DocumentViewer2));
		/// <summary>
		/// проблема - что бы отображать только страницы выбранные в диалоге
		/// нужно обернуть Paginator в другой Paginator который реализует логику выборки страниц
		/// однако если получить Paginator из FixedDocument и передать на печать это приведет к ошибке
		/// System.Windows.Xps.XpsSerializationException: FixedPage не может содержать другую FixedPage.
		/// что бы избежать этого получает оригинальный flowdocument из которого был создан fixeddocument
		/// и отправляем на печать его
		/// </summary>
		public PrintResult PrintResult;

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
			dialog.UserPageRangeEnabled = true;
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
			if (PrintResult != null)
				_writer.WriteAsync(PrintResult.GetPaginator(dialog));
			else if (Document is FixedDocumentSequence)
				_writer.WriteAsync(Document as FixedDocumentSequence);
			else if (Document is FixedDocument)
				_writer.WriteAsync(Document as FixedDocument);
			else
				_writer.WriteAsync(Document.DocumentPaginator);
		}

		private void Cancelled(object sender, WritingCancelledEventArgs e)
		{
			CleanUpWriter(e.Error);
		}

		private void Completed(object sender, WritingCompletedEventArgs e)
		{
			CleanUpWriter(e.Error);
		}

		private void CleanUpWriter(Exception e)
		{
			//кидать здесь исключение не кажется хорошей идей
			//поэтому делаем это только в дебаге что бы было понятно почему
			//ничего не печатает
			if (e != null && !(e is PrintingCanceledException)) {
#if DEBUG
				throw new Exception("Ошибка при печати документа", e);
#else
				log.Error("Ошибка при печати документа", e);
#endif
			}
			if (_writer == null)
				return;

			_writer.WritingCancelled -= Cancelled;
			_writer.WritingCompleted -= Completed;
			_writer = null;
			CommandManager.InvalidateRequerySuggested();
		}
	}
}