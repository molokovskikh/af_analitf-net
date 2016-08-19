using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;
using Caliburn.Micro;

using ILog = log4net.ILog;
using LogManager = log4net.LogManager;



namespace AnalitF.Net.Client.Controls
{
	public class FixedDocumentPaginator : DocumentPaginator
	{
		private FixedDocument doc;
		private FixedPage[] pages;

		public FixedDocumentPaginator(FixedDocument doc, FixedPage[] pages)
		{
			this.doc = doc;
			this.pages = pages;
		}

		public override DocumentPage GetPage(int pageNumber)
		{
			if (pageNumber < 0 || pageNumber >= pages.Length)
				return DocumentPage.Missing;
			return new DocumentPage(pages[pageNumber].Children[0], new Size(816.0, 1056.0), new Rect(0, 0, 816.0, 1056.0), new Rect(0, 0, 816.0, 1056.0));
		}

		public override bool IsPageCountValid
		{
			get { return true; }
		}

		public override int PageCount
		{
			get { return pages.Length; }
		}

		public override Size PageSize
		{
			get { return new Size(816.0, 1056.0); }
			set { throw new NotImplementedException(); }
		}

		public override IDocumentPaginatorSource Source
		{
			get { return doc; }
		}
	}

	public static class CustomDocPreviewCommands
	{
		public static RoutedUICommand SaveToFileCommand { get; } = new RoutedUICommand("Сохранить отчет в файл", "savetortf", typeof(CustomDocPreviewCommands));
	}

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
		public bool Busy { get; protected set; }

		public static DependencyProperty OrientationProperty
			= DependencyProperty.RegisterAttached("Orientation",
				typeof(PageOrientation),
				typeof(DocumentViewer2),
				new FrameworkPropertyMetadata(PageOrientation.Unknown));

		private XpsDocumentWriter _writer;

		static DocumentViewer2()
		{
			CommandManager.RegisterClassCommandBinding(typeof(DocumentViewer2),
				new CommandBinding(CustomDocPreviewCommands.SaveToFileCommand, Execute, CanExecute));
			CommandManager.RegisterClassInputBinding(typeof(DocumentViewer2),
				new InputBinding(CustomDocPreviewCommands.SaveToFileCommand, new KeyGesture(Key.S, ModifierKeys.Control)));

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
			if (e.Command == ApplicationCommands.Print || e.Command == CustomDocPreviewCommands.SaveToFileCommand) {
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
			else if (e.Command == CustomDocPreviewCommands.SaveToFileCommand)
				doc.SaveToFileCommand();
		}

		protected void SaveToFileCommand()
		{
			var printDoc = PrintResult.Docs.First().Value;
			if (printDoc == null)
				return ;
			try {
				Busy = true;
				var bd = printDoc.Item2;
				var baseFd = bd.Build();
				foreach (Block block in baseFd.Blocks) {
					if (block is Table) {
						//что бы в таблице rtf прорисовывались все линии
						Table table = block as Table;
						foreach (var rowGroup in table.RowGroups) {
							foreach (var currentRow in rowGroup.Rows) {
								foreach (var cell in currentRow.Cells) {
									cell.BorderThickness = new Thickness(0.5, 0.5, 0.5, 0.5);
									cell.BorderBrush = Brushes.Black;
								}
							}
						}
					}
				}
				var result = new SaveFileResult(new[] {
					Tuple.Create("Файл PNG (*.png)", ".png"),
					Tuple.Create("Файл PNG (страница) (*.png)", ".png"),
					Tuple.Create("Файл RTF (*.rtf)", ".rtf")
				});
				result.Execute(new ActionExecutionContext());
				if (result.Success) {
					if (result.Dialog.FilterIndex == 1) {
						var wdp = PrintResult.GetPaginator(PageRangeSelection.AllPages, new PageRange(0)) as WrapDocumentPaginator;
						var bitmaps = PrintHelper.ToBitmap(wdp, true);
						for (int i = 0; i < bitmaps.Count; i++) {
							BitmapFrame bmf = BitmapFrame.Create(bitmaps[i]);
							var enc = new PngBitmapEncoder();
							enc.Frames.Add(bmf);
							using (var fs = result.Stream($"_{i + 1}")) {
								enc.Save(fs);
							}
						}
					} else if (result.Dialog.FilterIndex == 2) {
						var wdp = PrintResult.GetPaginator(PageRangeSelection.AllPages, new PageRange(0)) as WrapDocumentPaginator;
						var bitmaps = PrintHelper.ToBitmap(wdp, false);
						for (int i = 0; i < bitmaps.Count; i++) {
							BitmapFrame bmf = BitmapFrame.Create(bitmaps[i]);
							var enc = new PngBitmapEncoder();
							enc.Frames.Add(bmf);
							using (var fs = result.Stream($"_{i + 1}")) {
								enc.Save(fs);
							}
						}
					} else if (result.Dialog.FilterIndex == 3) {
						using (var writer = result.Writer()) {
							var rtfString = PrintHelper.ToRtfString(baseFd, Orientation);
							writer.WriteLine(rtfString);
							writer.Flush();
							writer.Close();
						}
					}
				}
			} catch (Exception) {
				throw;
			} finally {
				Busy = false;
			}
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
			else if (Document is FixedDocument) {
				var fixedDocument = Document as FixedDocument;
				if (dialog.PageRangeSelection == PageRangeSelection.AllPages) {
					_writer.WriteAsync(fixedDocument);
				}
				else {
					var pages = new List<FixedPage>();
					var begin = Math.Max(0, dialog.PageRange.PageFrom - 1);
					var end = Math.Min(fixedDocument.Pages.Count, dialog.PageRange.PageTo);
					for(var i = begin; i < end; i++)
						pages.Add(fixedDocument.Pages[i].Child);
					_writer.WriteAsync(new FixedDocumentPaginator(fixedDocument, pages.ToArray()));
				}
			}
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