using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class DocModel<T> : BaseScreen, IPrintable where T : class, IDocModel
	{
		public DocModel(uint id)
		{
			Model = Session.Get<T>(id);
		}

		public T Model { get; set; }

		public FlowDocument Document { get; set; }

		public bool CanPrint
		{
			get { return Document != null; }
		}

		public bool CanSave
		{
			get { return Document != null; }
		}

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
			//и что бы увидеть данные пользователю нужно будет вызвать перерисовку документа, например с помощью скрола
			return new PrintResult(DisplayName, BuildDocument());
		}

		public IEnumerable<IResult> Save()
		{
			var formats = new[] {
				Tuple.Create("Rich Text Format", ".rtf"),
				Tuple.Create("Текстовый документ", ".txt"),
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
			if (Model == null)
				return null;

			return Model.ToFlowDocument();
		}
	}
}