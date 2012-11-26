using System;
using System.Collections.Generic;
using System.Windows;

namespace AnalitF.Net.Client.Extentions
{
	public class WindowManager : Caliburn.Micro.WindowManager
	{
		public bool UnderTest;
		public MessageBoxResult DefaultResult = MessageBoxResult.OK;
		public List<Window> Dialogs = new List<Window>();
		public List<string> MessageBoxes = new List<string>();

		public override bool? ShowDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			var window = CreateWindow(rootModel, true, context, settings);
			if (window.Owner != null) {
				window.SizeToContent = SizeToContent.Manual;
				window.Height = window.Owner.Height * 2 / 3;
				window.Width = window.Owner.Width * 2 / 3;
				window.ShowInTaskbar = false;
			}

			return ShowDialog(window);
		}

		public bool? ShowFixedDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			var window = CreateWindow(rootModel, true, context, settings);
			window.ResizeMode = ResizeMode.NoResize;
			window.SizeToContent = SizeToContent.WidthAndHeight;
			window.ShowInTaskbar = false;

			return ShowDialog(window);
		}

		private bool? ShowDialog(Window window)
		{
			if (UnderTest) {
				window.Closed += (sender, args) => Dialogs.Remove(window);
				Dialogs.Add(window);
				return true;
			}
			return window.ShowDialog();
		}

		public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
		{
			if (UnderTest) {
				MessageBoxes.Add(text);
				return DefaultResult;
			}

			return MessageBox.Show(text, caption, buttons, icon);
		}

		public void Warning(string text)
		{
			ShowMessageBox(text, "АналитФАРМАЦИЯ: Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		public void Notify(string text)
		{
			ShowMessageBox(text, "АналитФАРМАЦИЯ: Информация", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void Error(string text)
		{
			ShowMessageBox(text, "АналитФАРМАЦИЯ: Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}