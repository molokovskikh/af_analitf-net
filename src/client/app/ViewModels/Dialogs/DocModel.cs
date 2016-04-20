using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class TextDoc : IDocModel
	{
		public TextDoc(string displayName, string text)
		{
			DisplayName = displayName;
			Text = text;
		}

		public string Text { get; set; }

		public string DisplayName { get; set; }

		public FlowDocument ToFlowDocument()
		{
			var doc = new FlowDocument();
			doc.FontSize = 12;
			doc.FontFamily = new FontFamily("Arial");
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			//мы должны оставить место для "шапки" и "подвала"
			paginator.PageSize = new Size(paginator.PageSize.Width - WrapDocumentPaginator.Margins.Left - WrapDocumentPaginator.Margins.Right,
				paginator.PageSize.Height - WrapDocumentPaginator.Margins.Bottom - WrapDocumentPaginator.Margins.Top);

			doc.Blocks.Add(new Paragraph(new Bold(new Run("Предложения по данным позициям из заказа отсутствуют") { FontSize = 16})));
			foreach (var line in Text.Split(new [] { Environment.NewLine }, StringSplitOptions.None)) {
				doc.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0)});
			}
			return doc;
		}

		public void Init(Config.Config config)
		{
		}
	}

	public class DocModel<T> : BaseScreen, IPrintable where T : class, IDocModel
	{
		public DocModel(IDocModel docModel)
		{
			Model = docModel;
		}

		public DocModel(uint id)
		{
			Model = Session.Get<T>(id);
		}

		public IDocModel Model { get; set; }

		public FlowDocument Document { get; set; }

		public bool CanPrint => Document != null;

		public bool CanSave => Document != null;

		protected override void OnActivate()
		{
			base.OnActivate();

			if (Model != null) {
				Model.Init(Shell.Config);
				DisplayName = Model.DisplayName;
				Document = BuildDocument();
			}
		}

		public PrintResult Print()
		{
			//мы не можем использовать существующий документ, тк это приведет к тому что визуализация в FlowDocumentScrollViewer "исчезнет"
			//и что бы увидеть данные пользователю нужно будет вызвать перерисовку документа, например с помощью полосы прокрутки
			return new PrintResult(DisplayName, BuildDocument());
		}

		public IEnumerable<IResult> Save()
		{
			var formats = new[] {
				Tuple.Create("Rich Text Format (*.rtf)", ".rtf"),
				Tuple.Create("Текстовый документ (*.txt)", ".txt"),
			};
			var dialog = new SaveFileResult(formats, DisplayName);
			yield return dialog;
			var doc = BuildDocument();

			var range = new TextRange(doc.ContentStart, doc.ContentEnd);
			using (var stream = File.Create(dialog.Dialog.FileName)) {
				var format = DataFormats.Rtf;
				if (Path.GetExtension(dialog.Dialog.FileName).Match(".txt"))
					format = DataFormats.Text;
				range.Save(stream, format);
			}
		}

		public FlowDocument BuildDocument()
		{
			return Model?.ToFlowDocument();
		}
	}
}