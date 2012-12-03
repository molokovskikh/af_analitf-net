using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels
{
	public class DescriptionViewModel : BaseScreen
	{
		public DescriptionViewModel(ProductDescription description)
		{
			Description = description;
			DisplayName = "Описание";
			Document = BuildDocument();
		}

		public ProductDescription Description { get; set; }

		public FlowDocument Document { get; set; }

		public void Print()
		{
			//мы не можем использовать существующий документ, тк это приведет к тому что визуализация в FlowDocumentScrollViewer "исчезнет"
			//и что бы увидеть данные пользователю нужно будет вызвать перересовку документа, например с помощью скрола
			PrintDocument(BuildDocument(), DisplayName);
		}

		private FlowDocument BuildDocument()
		{
			var document = new FlowDocument();

			if (Description == null)
				return null;

			var properties = new List<Tuple<int, string, string>>();
			foreach (var property in Description.GetType().GetProperties()) {
				var attribute = property.GetCustomAttributes(typeof(DisplayAttribute), true)
					.OfType<DisplayAttribute>()
					.FirstOrDefault();

				if (attribute == null || string.IsNullOrEmpty(attribute.Name))
					continue;

				var value = property.GetValue(Description, null) as string;

				if (string.IsNullOrEmpty(value))
					continue;

				value = value.Trim();
				properties.Add(Tuple.Create(attribute.Order, attribute.Name, value));
			}

			document.Blocks.Add(new Paragraph(new Run(Description.FullName)) {
				FontWeight = FontWeights.Bold,
				FontSize = 20,
				TextAlignment = TextAlignment.Center
			});

			foreach (var property in properties.OrderBy(p => p.Item1)) {
				document.Blocks.Add(new Paragraph(new Run(property.Item2) { FontWeight = FontWeights.Bold }));
				document.Blocks.Add(new Paragraph(new Run(property.Item3)));
			}


			return document;
		}

		private static void PrintDocument(FlowDocument doc, string name)
		{
			if (doc == null)
				return;
			var dialog = new PrintDialog();
			if (dialog.ShowDialog() != true)
				return;
			Console.WriteLine("dialog.PrintableAreaWidth = " + dialog.PrintableAreaWidth);

			var maxWidth = dialog.PrintableAreaWidth;
			doc.PagePadding = new Thickness(25);
			doc.ColumnWidth = maxWidth;
			dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, name);
		}
	}
}