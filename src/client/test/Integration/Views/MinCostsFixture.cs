using System.IO;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class MinCostsFixture : BaseViewFixture
	{
		[Test]
		public void Spliter_settings()
		{
			var height = new GridLength();
			int diff;
			WpfTestHelper.WithWindow2(async w => {
				var model = new MinCosts();
				var view = Bind(model);
				w.Content = view;
				await view.WaitLoaded();
				var grid = (Grid)view.FindName("MainGrid");
				var row = grid.RowDefinitions[Grid.GetRow((UIElement)view.FindName("Costs"))];
				row.Height = new GridLength(row.ActualHeight + 5, GridUnitType.Pixel);
				height = row.Height;

				diff = model.Diff.Value = model.Diff.Value + 1;
				Close(model);
			});

			var memory = new MemoryStream();
			var streamWriter = new StreamWriter(memory);
			shell.Serialize(streamWriter);
			streamWriter.Flush();
			shell.PersistentContext.Clear();
			memory.Position = 0;
			shell.Deserialize(new StreamReader(memory));

			WpfTestHelper.WithWindow2(async w => {
				var view = Bind(new MinCosts());
				w.Content = view;
				var grid = (Grid)view.FindName("MainGrid");
				await view.WaitLoaded();
				Assert.AreEqual(height, grid.RowDefinitions[Grid.GetRow((UIElement)view.FindName("Costs"))].Height);
			});
		}
	}
}