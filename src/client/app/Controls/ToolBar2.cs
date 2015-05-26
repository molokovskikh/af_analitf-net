using System.Windows;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Controls
{
	/// <summary>
	/// обычный toolbar перегружает стиль кнопок нам это не нужно
	/// </summary>
	public class ToolBar2 : ToolBar
	{
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
		}
	}
}