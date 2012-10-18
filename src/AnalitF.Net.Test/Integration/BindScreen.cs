using System;
using System.Windows;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class BindScreen
	{
		[Test, RequiresSTA]
		public void Test()
		{
			AppBootstrapper.RegisterBinder();

			var view = new CatalogView();
			var model = new CatalogViewModel();
			ViewModelBinder.Bind(model, view, null);

			model.CurrentCatalogName = model.CatalogNames[0];
			Console.WriteLine(view.FindName("CatalogNames"));
			var catalogForms = (DataGrid)view.FindName("CatalogForms");
			var form = catalogForms.Items[0];
			catalogForms.SelectAll();

			//Console.WriteLine();
			Dump(view);

			//var generator = ((IItemContainerGenerator)catalogForms.ItemContainerGenerator);

			//using(generator.StartAt(new GeneratorPosition(0, 0), GeneratorDirection.Forward)) {
			//	var child = generator.GenerateNext();
			//	generator.PrepareItemContainer(child);
			//}

			//Console.WriteLine();
			//var row = catalogForms.ItemContainerGenerator.ContainerFromIndex(0);
			//row = catalogForms.ItemContainerGenerator.ContainerFromItem(form);
			//Console.WriteLine(row);
			//Console.WriteLine(form.GetType());
			//Console.WriteLine(catalogForms);
		}



		public void Dump(DependencyObject o, string pad = "")
		{
			Console.WriteLine(pad + o);
			foreach (var child in XamlExtentions.Children(o)) {
				Dump(child, pad + "\t");
			}
		}
	}
}