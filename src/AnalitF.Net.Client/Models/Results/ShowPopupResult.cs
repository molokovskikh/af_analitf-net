using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class ShowPopupResult : IResult
	{
		public void Execute(ActionExecutionContext context)
		{
			var cell = Keyboard.FocusedElement as DataGridCell;
			if (cell != null) {
				var menu = new ContextMenu {
					Items = {
						new MenuItem { FontWeight = FontWeights.Bold, Header = "Нет предложений" },
						new MenuItem { Header = "Показать историю заказов"}
					}
				};
				menu.IsOpen = true;
				menu.PlacementTarget = cell;
				menu.Placement = PlacementMode.Bottom;
				var args = HandledResult.ExtractKeyEventArgs(context);
				var keyboard = args != null ? args.KeyboardDevice : Keyboard.PrimaryDevice;
				var timestam = args != null ? args.Timestamp : (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
				var inputSource = args != null ? args.InputSource : PresentationSource.FromDependencyObject(context.View);
				//устанавливаем курсор на первую строку
				menu.RaiseEvent(new KeyEventArgs(keyboard, inputSource, timestam, Key.Down) {
					RoutedEvent = UIElement.KeyDownEvent
				});
			}
			if (Completed != null) {
				Completed(this, new ResultCompletionEventArgs());
			}
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}